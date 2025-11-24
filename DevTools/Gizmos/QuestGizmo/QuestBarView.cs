using ISILab.Commons.Utility.Editor;
using System.Collections.Generic;
using System.Linq;
using ISI_Lab.LBS.DevTools;
using ISILab.LBS.Components;
using ISILab.LBS.Macros;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Modules;
using UnityEditor;

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
        #region VIEW FIELDS
        private readonly Button _nextStep;
        #endregion

        #region FIELDS
        // 0 = start, 1 = end
        private const float ButtonLineRatioPos = 0.5f; 
        private readonly QuestTrigger _trigger;
        private readonly QuestTracker _tracker;
        private static readonly List<VisualElementWorld> PrevButtons = new();

        private static readonly string StartIconGuid = "6f8a8cf2b556996428f482386e991352";
        private static readonly string GoalIconGuid = "91e56097e660ca548b3337ccfa31b752";
        #endregion

        public QuestBarView(QuestTracker tracker, QuestTrigger trigger,  Custom3dQuestGizmo questGizmo)
        {
            if(trigger is null) return;
            if(trigger.Node is null) return;
            
            VisualTreeAsset view = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestBarView");
            view.CloneTree(this);

            VisualElement previousContainer = this.Q<VisualElement>("Previous");
            VisualElement nextContainer = this.Q<VisualElement>("Next");
            
            Button previousStep = this.Q<Button>("PreviousStep");
            _nextStep = this.Q<Button>("NextStep");
            Label action = this.Q<Label>("Action");
            VisualElement stepType = this.Q<VisualElement>("StepType");

            previousStep.style.display = DisplayStyle.Flex;
            _nextStep.clicked += NextStepOnClicked;
            
            _tracker = tracker;
            _trigger = trigger;
            
            action.text = trigger.Node.QuestAction;
            
            if(trigger.Node.NodeType == QuestNode.ENodeType.Middle) stepType.style.display = DisplayStyle.None;
            else
            {
                stepType.style.display = DisplayStyle.Flex;
                if (trigger.Node.NodeType == QuestNode.ENodeType.Start)
                {
                    stepType.style.backgroundImage =
                        new StyleBackground(LBSAssetMacro.LoadAssetByGuid<VectorImage>(StartIconGuid));
                    previousContainer.style.display = DisplayStyle.None;
                }
                if(trigger.Node.NodeType == QuestNode.ENodeType.Goal)
                {
                    stepType.style.backgroundImage =
                        new StyleBackground(LBSAssetMacro.LoadAssetByGuid<VectorImage>(GoalIconGuid));
                    nextContainer.style.display = DisplayStyle.None;
                }
            }
            
            questGizmo.prevTriggers.Clear();

            if (tracker is null) return;

            QuestTrigger[] questObjects = tracker.GetComponentsInChildren<QuestTrigger>();
            foreach (QuestEdge qe in tracker.QuestGraph.GraphEdges)
            {
                if (!Equals(qe.To, trigger.Node)) continue;
                
                previousStep.clicked += ()=> OnPrevStepClicked(questObjects.First());
                
                foreach (QuestTrigger qt in questObjects)
                {
                    if (!qe.From.Contains(qt.Node)) continue;
                    
                    // for line drawing
                    questGizmo.prevTriggers.Add(qt);
                    
                    VisualElement prevButton = new PrevStepButton(previousStep, this, qt);
                            
                    SceneView.lastActiveSceneView.rootVisualElement.Add(prevButton);
                            
                    Vector3 fromPos = qt.transform.position;
                    Vector3 toPos = trigger.transform.position;
                    Vector3 buttonPos = Vector3.Lerp(fromPos, toPos, ButtonLineRatioPos);
                    DebugButtonPosition(prevButton, buttonPos);
                    VisualElementWorld entry = new(buttonPos, prevButton);
                    PrevButtons.Add(entry);
                }
            }
            
            MarkDirtyRepaint();
        }
        
        public static void ClearPreviousButtons()
        {
            // Always check for existing scene views
            foreach (var sceneView in SceneView.sceneViews)
            {
                if (sceneView is not SceneView sv || sv.rootVisualElement == null)
                    continue;

                foreach (VisualElementWorld ve in PrevButtons)
                {
                    if (ve.Element == null) continue;

                    // Check if the button is actually a child of this SceneView
                    if (ve.Element.hierarchy.parent == sv.rootVisualElement)
                    {
                        sv.rootVisualElement.Remove(ve.Element);
                    }
                }
            }

            PrevButtons.Clear();
        }


        private void DebugButtonPosition(VisualElement button, Vector3 worldPos)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (!sceneView || !sceneView.camera) return;

            Camera cam = sceneView.camera;
            Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);

            // If behind camera, hide
            if (screenPoint.z < 0f)
            {
                button.style.display = DisplayStyle.None;
                return;
            }
            
            float uiX = screenPoint.x;
            // minus the UI invert Y
            float uiY = sceneView.position.height - screenPoint.y;
            
            // Scaling
            float ppp = EditorGUIUtility.pixelsPerPoint;
            uiX /= ppp;
            uiY /= ppp;
            
            const float buttonSize = 50f;
            button.style.position = Position.Absolute;
            button.style.left = uiX - (buttonSize * 0.5f);
            // subtract offset (UIToolkit has reversed Y)
            button.style.top  = uiY - buttonSize;
    
            button.style.display = DisplayStyle.Flex;
        }
        
        #region BUTTON METHODS
        private void NextStepOnClicked()
        {
            QuestTrigger[] questObjects = _tracker.GetComponentsInChildren<QuestTrigger>();
            foreach (QuestEdge qe in _tracker.QuestGraph.GraphEdges)
            {
                if (qe.From.Contains(_trigger.Node))
                {
                    foreach (QuestTrigger qt in questObjects)
                    {
                        if (Equals(qt.Node, qe.To))
                        {
                            SelectQuestObject(qt);
                            return;
                        }
                    }
                }
            }
        }

        private static void SelectQuestObject(QuestTrigger qt)
        {
            if(qt is null) return;
            GameObject destination = qt.gameObject;
                     
            // Select it in the Hierarchy
            Selection.activeGameObject = destination;

            // Frame it in the Scene view
            SceneView.lastActiveSceneView.FrameSelected(true);
            
            EditorGUIUtility.PingObject(destination);
        }

        public void UpdatePreviousButtons()
        {
            //  Update positions
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (!sceneView) return;

            foreach (VisualElementWorld item in PrevButtons)
            {
                if (item.Element != null)
                {
                    DebugButtonPosition(item.Element, item.Position);
                }
            }
        }

        public void OnPrevStepClicked(QuestTrigger qt)
        {
            SelectQuestObject(qt);
        }
        #endregion
        
    }
}