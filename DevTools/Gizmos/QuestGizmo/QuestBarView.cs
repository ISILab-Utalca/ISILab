using ISILab.Commons.Utility.Editor;
using System.Collections.Generic;
using ISI_Lab.LBS.DevTools;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Modules;
using UnityEditor;

namespace ISILab.LBS.VisualElements
{
    public class QuestBarView : GraphElement
    {
        #region VIEW FIELDS
        private static VisualTreeAsset view;
        
        private Button PreviousStep;
        private Button NextStep;
        private VisualElement StepType;
        private Label Action;
        
        // 0 = start, 1 = end
        private const float buttonLineRatioPos = 0.5f; 


        // reference to the gameObject containing the gizmo
        private GameObject go;
        
        // other references
        private QuestTrigger trigger;
        private QuestTracker tracker;
        private Custom3dQuestGizmo questGizmo;
        
        [HideInInspector]
        public List<VisualElement> PrevButtons = new();
        
        #endregion

        public QuestBarView(QuestTracker tracker, QuestTrigger trigger,  Custom3dQuestGizmo questGizmo)
        {
            view = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestBarView");
            view.CloneTree(this);

            PreviousStep = this.Q<Button>("PreviousStep");
            NextStep = this.Q<Button>("NextStep");
            Action = this.Q<Label>("Action");
            StepType = this.Q<VisualElement>("StepType");

            PreviousStep.style.display = DisplayStyle.Flex;
            PreviousStep.SetEnabled(false);
            NextStep.clicked += NextStepOnClicked;
            
            this.tracker = tracker;
            this.trigger = trigger;
            this.questGizmo = questGizmo;
            
            go = trigger.gameObject;
            
            Action.text = trigger.Node.QuestAction;
            
            foreach (VisualElement ve in PrevButtons)
            {
                if (ve != null && ve.hierarchy.parent == this)  Remove(ve);
            }
            PrevButtons.Clear();
    
            questGizmo.Positions.Clear();

            if (tracker is null) return;

            QuestTrigger[] questObjects = tracker.GetComponentsInChildren<QuestTrigger>();
            foreach (QuestEdge qe in tracker.QuestGraph.GraphEdges)
            {
                if (Equals(qe.To, trigger.Node))
                {
                    foreach (QuestTrigger qt in questObjects)
                    {
                        if (qe.From.Contains(qt.Node))
                        {
                            // for line drawing
                            questGizmo.Positions.Add(qt.transform.position);
                            
                            VisualElement prevButton = new PrevStepButton(PreviousStep, this, qt);
                         
                            PrevButtons.Add(prevButton);
                            Add(prevButton);
                            
                            Vector3 fromPos = qt.transform.position;
                            Vector3 toPos = trigger.transform.position;
                            float t = buttonLineRatioPos;
                            Vector3 buttonPos = Vector3.Lerp(fromPos, toPos, t);
                
                            // convert to GUI space
                            SceneView sceneView = SceneView.lastActiveSceneView;
                            if (!sceneView || !sceneView.camera) continue;
                
                            Vector3 screenPos = sceneView.camera.WorldToScreenPoint(buttonPos);
                            Vector2 guiPos = new Vector2(screenPos.x, sceneView.position.height - screenPos.y); 

                            prevButton.style.left = guiPos.x - 25; // center offset
                            prevButton.style.top = guiPos.y - 25;
                            prevButton.style.width = 50;
                            prevButton.style.height = 50;
                        }
                    }
                }
            }
            
            MarkDirtyRepaint();
        }
        
        #region BUTTON METHODS
        private void NextStepOnClicked()
        {
            QuestTrigger[] questObjects = tracker.GetComponentsInChildren<QuestTrigger>();
            foreach (QuestEdge qe in tracker.QuestGraph.GraphEdges)
            {
                if (qe.From.Contains(trigger.Node))
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
            GameObject destination = qt.gameObject;
                     
            // Select it in the Hierarchy
            Selection.activeGameObject = destination;

            // Frame it in the Scene view
            SceneView.lastActiveSceneView.FrameSelected();

            // Optionally ping it in the project window (nice UX touch)
            EditorGUIUtility.PingObject(destination);
        }

        public void UpdatePreviousButtons()
        {
            // Cleanup old
          
        }

        public void OnPrevStepClicked(QuestTrigger qt)
        {
            SelectQuestObject(qt);
        }
        #endregion
        
    }
}