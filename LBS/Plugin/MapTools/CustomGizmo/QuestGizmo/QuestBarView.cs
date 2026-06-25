using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.CustomGizmo.QuestGizmo;
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
        private static readonly string PrevArrowIconGuid = "154cf402299c4144d95a7b5e4550ca11";
        private static readonly string NextArrowIconGuid = "f7a7e78297daae54d8533114b779de1f";

        private readonly Button previousStep;
        private readonly Button nextStep;
        #endregion

        #region PROPERTIES
        private static VectorImage prevIcon;
        private static VectorImage nextIcon;

        private static VectorImage PrevIcon => prevIcon ??= AssetMacro.LoadAssetByGuid<VectorImage>(PrevArrowIconGuid);
        private static VectorImage NextIcon => nextIcon ??= AssetMacro.LoadAssetByGuid<VectorImage>(NextArrowIconGuid);
        #endregion

        public QuestBarView(Custom3dQuestGizmo questGizmo)
        {
            if (questGizmo?.Trigger == null) return;
            trigger = questGizmo.Trigger;

            VisualTreeAsset view = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestBarView");
            if (view == null) return;
            view.CloneTree(this);

            // Fetch and bind basic UI elements
            previousStep = this.Q<Button>("PreviousStep");
            nextStep = this.Q<Button>("NextStep");
            Label action = this.Q<Label>("Action");

            previousStep.style.display = DisplayStyle.Flex;
            previousStep.clicked += PrevStepOnClicked;
            nextStep.clicked += NextStepOnClicked;

            action.text = trigger.ToString();
            action.style.display = DisplayStyle.Flex;

            // Handle Node-specific configurations (Start, Goal, Middle)
            if (trigger is QuestTriggerNode qtn)
            {
                SetupNodeTypeVisuals(qtn);
            }

            // Generate Navigational Buttons
            if (trigger.Previous != null && PrevIcon != null)
            {
                foreach (QuestTrigger prev in trigger.Previous.Where(p => p != null))
                {
                    CreateNavButton(prev, PrevIcon, PrevButtons, "Previous");
                }
            }

            if (trigger.Next != null && NextIcon != null)
            {
                foreach (QuestTrigger next in trigger.Next.Where(n => n != null))
                {
                    CreateNavButton(next, NextIcon, NextButtons, "Next");
                }
            }

            MarkDirtyRepaint();
        }

        #region HELPER INITIALIZERS
        private void SetupNodeTypeVisuals(QuestTriggerNode qtn)
        {
            VisualElement stepType = this.Q<VisualElement>("StepType");
            VisualElement previousContainer = this.Q<VisualElement>("Previous");
            VisualElement nextContainer = this.Q<VisualElement>("Next");

            QuestNode.NodeGraphType nType = qtn.NodeType;

            if (nType == QuestNode.NodeGraphType.Middle)
            {
                stepType.style.display = DisplayStyle.None;
                return;
            }

            stepType.style.display = DisplayStyle.Flex;
            string iconGuid = (nType == QuestNode.NodeGraphType.Start) ? StartIconGuid : GoalIconGuid;
            stepType.style.backgroundImage = new StyleBackground(AssetMacro.LoadAssetByGuid<VectorImage>(iconGuid));

            if (nType == QuestNode.NodeGraphType.Start)
            {
                previousStep.style.display = DisplayStyle.None;
                previousContainer.style.display = DisplayStyle.None;
            }
            else if (nType == QuestNode.NodeGraphType.Goal)
            {
                nextStep.style.display = DisplayStyle.None;
                nextContainer.style.display = DisplayStyle.None;
            }
        }
        #endregion

        #region NAVIGATION BUTTONS LOGIC
        private void CreateNavButton(QuestTrigger targetTrigger, VectorImage icon, List<VisualElementWorld> buttonList, string tooltipText)
        {
            if (targetTrigger?.gameObject == null) return;

            SceneView sv = SceneView.lastActiveSceneView;
            if (sv?.rootVisualElement == null) return;

            var newButton = new Button(() => SelectTriggerGameObject(targetTrigger))
            {
                iconImage = new Background() { vectorImage = icon },
                tooltip = tooltipText
            };

            sv.rootVisualElement.Add(newButton);

            Vector3 buttonPos = Vector3.Lerp(
                targetTrigger.gameObject.transform.position,
                trigger.gameObject.transform.position,
                ButtonLineRatioPos);

            buttonList.Add(new VisualElementWorld(buttonPos, newButton));
            UpdatePosition(newButton, buttonPos);
        }

        private void PrevStepOnClicked() => SelectTriggerGameObject(trigger?.Previous?.FirstOrDefault());
        private void NextStepOnClicked() => SelectTriggerGameObject(trigger?.Next?.FirstOrDefault());

        public static void SelectTriggerGameObject(QuestTrigger qt)
        {
            if (qt?.gameObject == null) return;

            Selection.activeGameObject = qt.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected(false);
            EditorGUIUtility.PingObject(qt.gameObject);
        }
        #endregion

        #region LAYOUT POSITION UPDATES
        public void UpdatePositions()
        {
            UpdateCollectionPositions(PrevButtons);
            UpdateCollectionPositions(NextButtons);
        }

        private void UpdateCollectionPositions(List<VisualElementWorld> buttons)
        {
            foreach (var item in buttons)
            {
                if (item.Element != null)
                {
                    UpdatePosition(item.Element, item.Position);
                }
            }
        }

        private void UpdatePosition(VisualElement button, Vector3 worldPos)
        {
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv?.camera == null || button == null) return;

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
            ClearButtonCollection(PrevButtons);
            ClearButtonCollection(NextButtons);
        }

        private static void ClearButtonCollection(List<VisualElementWorld> buttons)
        {
            foreach (var ve in buttons) ve.Element?.RemoveFromHierarchy();
            buttons.Clear();
        }
        #endregion
    }
}