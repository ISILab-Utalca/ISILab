using ISI_Lab.LBS.DevTools;
using ISILab.LBS;
using ISILab.LBS.VisualElements;
using UnityEngine.UIElements;
using UnityEditor;

namespace ISI_Lab.DevTools.Gizmos.Editor
{
    [CustomEditor(typeof(Custom3dQuestGizmo))]
    public class Custom3dQuestGizmoEditor : Custom3DGizmoEditorBase<Custom3dQuestGizmo>
    {
        protected override VisualElement CreateInspectorUI()
        {
            rootVisualElement = new QuestBarView(TargetGizmo.Tracker, TargetGizmo.Trigger, TargetGizmo);
            return rootVisualElement;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            QuestBarView.ClearPreviousButtons();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            QuestBarView.ClearPreviousButtons();
        }

        protected override void OnUpdate(SceneView sceneView)
        {
            QuestBarView qbv = rootVisualElement as  QuestBarView;
            qbv?.UpdatePreviousButtons();
        }
    }
}


/*
using ISI_Lab.LBS.DevTools;
using ISI_Lab.LBS.Plugin.MapTools.Generators3D;
using ISILab.LBS;
using ISILab.LBS.VisualElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISI_Lab.DevTools.Gizmos.Editor
{
    
    [CustomEditor(typeof(Custom3dQuestGizmo))]
    public class Custom3dQuestGizmoEditor : UnityEditor.Editor
    {
        public VisualElement rootVisualElement;
        private bool isVisible;
        private Rect popupRect;

        private const float buttonSize = 18;
        private const float yOffset = 200;

        private QuestTrigger trigger;
        private QuestTracker tracker;
        
        void OnEnable()
        {
            Custom3dQuestGizmo targetComponent = (Custom3dQuestGizmo)target;
            trigger = targetComponent.gameObject.GetComponent<QuestTrigger>();
            tracker = targetComponent.Tracker;
            
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            RemoveUI();
        }
        
        private void RemoveUI()
        {
            if (rootVisualElement != null)
            {
                rootVisualElement.RemoveFromHierarchy();
                rootVisualElement = null;
            }
        }
        void OnSceneGUI(SceneView sceneView)
        {
            if (sceneView.drawGizmos)
            {
                Custom3dQuestGizmo targetComponent = (Custom3dQuestGizmo)target;
                targetComponent.UpdatePosition();
                Vector3 center = targetComponent.worldPosition;
                Vector2 screenPoint = HandleUtility.WorldToGUIPoint(center);

                if (rootVisualElement == null)
                {
                    rootVisualElement = new QuestBarView(tracker, trigger, targetComponent);
                    QuestBarView qbv = rootVisualElement as  QuestBarView;
                    UpdatePopupPosition(screenPoint);
                    
                    sceneView.rootVisualElement.Add(rootVisualElement);
                    qbv?.SetQuestInfo(tracker, trigger);
               
                }
                else
                {
                    // Update position
                    UpdatePopupPosition(screenPoint);
                    QuestBarView qbv = rootVisualElement as  QuestBarView;
                    qbv?.UpdateFroms();
                }

            }
            else
            {
                RemoveUI();
            }
        }

        private void UpdatePopupPosition(Vector2 screenPoint)
        {
            rootVisualElement.style.position = Position.Absolute;
            rootVisualElement.style.left = screenPoint.x - rootVisualElement.style.width.value.value/2;
            rootVisualElement.style.top = screenPoint.y - yOffset;
        }

        void DrawPopupWindow(int windowID)
        {
            GUILayout.Label("This is a popup in the Scene view.");
            if (GUILayout.Button("Close"))
            {
                isVisible = false;
            }

            // Make the window draggable
            GUI.DragWindow();
        }

    }
}
*/
