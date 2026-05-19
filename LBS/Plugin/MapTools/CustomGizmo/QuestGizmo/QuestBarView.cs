using ISILab.Commons.Utility.Editor;
using System.Collections.Generic;
using System.Linq;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.MapTools.CustomGizmo.QuestGizmo;
using ISILab.LBS.Plugin.MapTools.Gizmos.QuestGizmo;
using UnityEditor;
using ISILab.LBS.Plugin.MapTools.Generators;
using System;

namespace ISILab.LBS.VisualElements
{
    /// <summary>
    /// Simple container class to store world positions and a visual element
    /// </summary>
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
        private readonly QuestTrigger _trigger;
        private readonly QuestTracker _tracker;
        private static readonly List<VisualElementWorld> PrevButtons = new();

        private static readonly string StartIconGuid = "6f8a8cf2b556996428f482386e991352";
        private static readonly string GoalIconGuid = "91e56097e660ca548b3337ccfa31b752";
        #endregion

        public QuestBarView(Custom3dQuestGizmo questGizmo)
        {
            if (questGizmo is null || questGizmo.Trigger is null) 
                return;

            _trigger = questGizmo.Trigger;

            VisualTreeAsset view = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestBarView");
            view.CloneTree(this);

            VisualElement previousContainer = this.Q<VisualElement>("Previous");
            VisualElement nextContainer = this.Q<VisualElement>("Next");

            Button previousStep = this.Q<Button>("PreviousStep");
            Button nextStep = this.Q<Button>("NextStep"); 
            Label action = this.Q<Label>("Action");
            VisualElement stepType = this.Q<VisualElement>("StepType");

            previousStep.style.display = DisplayStyle.Flex;
            action.style.display = DisplayStyle.None;

            previousStep.clicked += PrevStepOnClicked;
            nextStep.clicked += NextStepOnClicked;

            if(_trigger is QuestTriggerNode qtn)
            {
                // Use the Terminal ID cached in the trigger
                action.text = qtn.Terminal.id;
                action.style.display = DisplayStyle.Flex;
                QuestNode.ENodeType nType = qtn.NodeType;


                // Handle icons using the cached NodeType enum
                if (nType == QuestNode.ENodeType.Middle)
                    stepType.style.display = DisplayStyle.None;
                else
                {
                    stepType.style.display = DisplayStyle.Flex;
                    string iconGuid = nType == QuestNode.ENodeType.Start ? StartIconGuid : GoalIconGuid;
                    stepType.style.backgroundImage = new StyleBackground(AssetMacro.LoadAssetByGuid<VectorImage>(iconGuid));

                    if (nType == QuestNode.ENodeType.Start)
                    {        
                        previousStep.style.display = DisplayStyle.None;
                        previousContainer.style.display = DisplayStyle.None;
                    }
                    else if (nType == QuestNode.ENodeType.Goal)
                    {
                        nextStep.style.display = DisplayStyle.None;
                        nextContainer.style.display = DisplayStyle.None;
                    }
                }


            }

            questGizmo.prevTriggers.Clear();
            foreach (QuestTrigger prev in _trigger.AllPrevious)
            {
                questGizmo.prevTriggers.Add(prev);

                // Create jumping button for scene navigation
                var prevButton = new Button(() => SelectTriggerGameObject(prev)) { text = "◄" };
                SceneView.lastActiveSceneView.rootVisualElement.Add(prevButton);

                Vector3 buttonPos = Vector3.Lerp(
                    prev.gameObject.transform.position, 
                    _trigger.gameObject.transform.position, 
                    ButtonLineRatioPos);

                PrevButtons.Add(new VisualElementWorld(buttonPos, prevButton));

                UpdatePosition(prevButton, buttonPos);
            }
           

            MarkDirtyRepaint();
        }

        
        private void PrevStepOnClicked() => SelectTriggerGameObject(_trigger?.AllPrevious?.FirstOrDefault());

        private void NextStepOnClicked() => SelectTriggerGameObject(_trigger != null ? _trigger.Next : null);

        public static void SelectTriggerGameObject(QuestTrigger qt)
        {
            if (qt == null) 
                return;

            Selection.activeGameObject = qt.gameObject;
            SceneView.lastActiveSceneView.FrameSelected(false);
            EditorGUIUtility.PingObject(qt.gameObject);
        }

        public void UpdatePositions()
        {
            foreach (var item in PrevButtons)
            {
                if (item.Element != null) UpdatePosition(item.Element, item.Position);
            }
        }

        private void UpdatePosition(VisualElement button, Vector3 worldPos)
        {
            SceneView sv = SceneView.lastActiveSceneView;
            if (!sv || !sv.camera) return;

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

        public static void ClearPreviousButtons()
        {
            foreach (var ve in PrevButtons) ve.Element?.RemoveFromHierarchy();
            PrevButtons.Clear();
        }

    }
}