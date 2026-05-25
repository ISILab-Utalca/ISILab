using ISILab.Commons.Utility.Editor;
using System.Collections.Generic;
using System.Linq;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Plugin.MapTools.CustomGizmo.QuestGizmo;
using UnityEditor;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.LBS.VisualElements
{
    public struct VisualElementWorld
    {
        public Vector3 Position;
        public readonly VisualElement Element;
        public VisualElementWorld(Vector3 position, VisualElement element)
        {
            Position = position;
            Element = element;
        }
    }

    public class QuestBarView : GraphElement
    {
        #region FIELDS
        private const float ButtonLineRatioPos = 0.5f;
        private readonly QuestTrigger trigger;
        private static readonly List<VisualElementWorld> PrevButtons = new();
        private static readonly List<VisualElementWorld> NextButtons = new();

        private static readonly string StartIconGuid = "6f8a8cf2b556996428f482386e991352";
        private static readonly string GoalIconGuid = "91e56097e660ca548b3337ccfa31b752";

        // Explicit asset paths/GUIDs for your step-navigation arrows
        private static readonly string PrevArrowIconGuid = "154cf402299c4144d95a7b5e4550ca11";
        private static readonly string NextArrowIconGuid = "f7a7e78297daae54d8533114b779de1f";

        private readonly Button previousStep;
        private readonly Button nextStep;
        #endregion

        #region STATIC
        private static VectorImage prevIcon;
        private static VectorImage nextIcon;

        // Safely pull directly from AssetDatabase macros rather than the layout instance styles
        private static VectorImage PrevIcon => prevIcon ??= AssetMacro.LoadAssetByGuid<VectorImage>(PrevArrowIconGuid);
        private static VectorImage NextIcon => nextIcon ??= AssetMacro.LoadAssetByGuid<VectorImage>(NextArrowIconGuid);
        #endregion

        public QuestBarView(Custom3dQuestGizmo questGizmo)
        {
            // Complete null safety block
            if (questGizmo is null || questGizmo.Trigger is null)
                return;

            trigger = questGizmo.Trigger;

            VisualTreeAsset view = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestBarView");
            if (view == null) return;

            view.CloneTree(this);

            VisualElement previousContainer = this.Q<VisualElement>("Previous");
            VisualElement nextContainer = this.Q<VisualElement>("Next");

            previousStep = this.Q<Button>("PreviousStep");
            nextStep = this.Q<Button>("NextStep");
            Label action = this.Q<Label>("Action");
            VisualElement stepType = this.Q<VisualElement>("StepType");

            if (previousStep != null)
            {
                previousStep.style.display = DisplayStyle.Flex;
                previousStep.clicked += PrevStepOnClicked;
            }

            if (nextStep != null)
            {
                nextStep.clicked += NextStepOnClicked;
            }

            if (action != null)
            {
                action.style.display = DisplayStyle.None;
            }

            if (trigger is QuestTriggerNode qtn)
            {
                if (action != null && qtn.Terminal != null)
                {
                    action.text = qtn.Terminal.id;
                    action.style.display = DisplayStyle.Flex;
                }

                QuestNode.ENodeType nType = qtn.NodeType;

                if (nType == QuestNode.ENodeType.Middle)
                {
                    if (stepType != null) stepType.style.display = DisplayStyle.None;
                }
                else
                {
                    if (stepType != null)
                    {
                        stepType.style.display = DisplayStyle.Flex;
                        string iconGuid = nType == QuestNode.ENodeType.Start ? StartIconGuid : GoalIconGuid;
                        stepType.style.backgroundImage = new StyleBackground(AssetMacro.LoadAssetByGuid<VectorImage>(iconGuid));
                    }

                    if (nType == QuestNode.ENodeType.Start)
                    {
                        if (previousStep != null) previousStep.style.display = DisplayStyle.None;
                        if (previousContainer != null) previousContainer.style.display = DisplayStyle.None;
                    }
                    else if (nType == QuestNode.ENodeType.Goal)
                    {
                        if (nextStep != null) nextStep.style.display = DisplayStyle.None;
                        if (nextContainer != null) nextContainer.style.display = DisplayStyle.None;
                    }
                }
            }

            // Ensure our static arrow images are fetched safely before executing button building loops
            VectorImage pIcon = PrevIcon;
            VectorImage nIcon = NextIcon;

            if (trigger.Previous != null && pIcon != null)
            {
                foreach (QuestTrigger prev in trigger.Previous)
                {
                    if (prev == null) continue;
                    CreateNavButton(prev, pIcon, PrevButtons);
                }
            }

            if (trigger.Next != null && nIcon != null)
            {
                foreach (QuestTrigger next in trigger.Next)
                {
                    if (next == null) continue;
                    CreateNavButton(next, nIcon, NextButtons);
                }
            }

            MarkDirtyRepaint();
        }

        void CreateNavButton(QuestTrigger targetTrigger, VectorImage icon, List<VisualElementWorld> buttonList)
        {
            if (targetTrigger == null || targetTrigger.gameObject == null) return;

            var sv = SceneView.lastActiveSceneView;
            if (sv == null || sv.rootVisualElement == null) return;

            var newButton = new Button(() => SelectTriggerGameObject(targetTrigger))
            {
                iconImage = new Background() { vectorImage = icon }
            };

            sv.rootVisualElement.Add(newButton);

            Vector3 buttonPos = Vector3.Lerp(
                targetTrigger.gameObject.transform.position,
                trigger.gameObject.transform.position,
                ButtonLineRatioPos);

            buttonList.Add(new VisualElementWorld(buttonPos, newButton));
            UpdatePosition(newButton, buttonPos);
        }

        private void PrevStepOnClicked()
        {
            if (trigger == null) return;
            SelectTriggerGameObject(trigger.Previous?.FirstOrDefault());
        }

        private void NextStepOnClicked()
        {
            if (trigger == null) return;
            SelectTriggerGameObject(trigger.Next?.FirstOrDefault());
        }

        public static void SelectTriggerGameObject(QuestTrigger qt)
        {
            if (qt == null || qt.gameObject == null) return;

            Selection.activeGameObject = qt.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected(false);
            EditorGUIUtility.PingObject(qt.gameObject);
        }

        public void UpdatePositions()
        {
            foreach (var item in PrevButtons)
            {
                if (item.Element != null)
                    UpdatePosition(item.Element, item.Position);
            }

            foreach (var item in NextButtons)
            {
                if (item.Element != null)
                    UpdatePosition(item.Element, item.Position);
            }
        }

        private void UpdatePosition(VisualElement button, Vector3 worldPos)
        {
            SceneView sv = SceneView.lastActiveSceneView;
            if (!sv || !sv.camera || button == null)
                return;

            Vector3 screenPoint = sv.camera.WorldToScreenPoint(worldPos);
            if (screenPoint.z < 0f)
            {
                button.style.display = DisplayStyle.None;
                return;
            }

            float ppp = EditorGUIUtility.pixelsPerPoint;
            button.style.left = (screenPoint.x / ppp) - 25f;
            button.style.top = ((sv.position.height - screenPoint.y) / ppp) - 25f;
            button.style.position = Position.Absolute;
            button.style.display = DisplayStyle.Flex;
        }

        public static void ClearButtons()
        {
            foreach (var ve in PrevButtons)
                ve.Element?.RemoveFromHierarchy();

            PrevButtons.Clear();

            foreach (var ve in NextButtons)
                ve.Element?.RemoveFromHierarchy();

            NextButtons.Clear();
        }
    }
}