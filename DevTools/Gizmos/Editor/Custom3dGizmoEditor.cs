using ISI_Lab.LBS.DevTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISI_Lab.DevTools.Gizmos.Editor
{
    /// <summary>
    /// Generic base class for 3D gizmo editors using UI Toolkit overlays.
    /// Handles SceneView UI positioning and lifecycle.
    /// </summary>
    public abstract class Custom3DGizmoEditorBase<T> : UnityEditor.Editor where T : Custom3dGizmo
    {
        protected VisualElement rootVisualElement;
        protected T TargetGizmo;
        protected bool isVisible;
        protected Rect popupRect;
        private const float yOffset = 200f;

        // Subscribe to SceneView events
        protected virtual void OnEnable()
        {
            TargetGizmo = (T)target;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        protected virtual void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            RemoveUI();
        }

        protected void RemoveUI()
        {
            if (rootVisualElement == null) return;
            
            rootVisualElement.RemoveFromHierarchy();
            rootVisualElement = null;
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!sceneView.drawGizmos || TargetGizmo is null)
            {
                RemoveUI();
                return;
            }
            
            // Update the gizmo’s position before drawing
            TargetGizmo.UpdatePosition();

            Vector3 center = TargetGizmo.worldPosition;
            Vector2 screenPoint = HandleUtility.WorldToGUIPoint(center);

            // Create UI if it doesn't exist yet
            if (rootVisualElement == null)
            {
                // assign the target gizmo element if assigned or create from the editor override
                rootVisualElement = TargetGizmo.RootVisualElement ?? CreateInspectorUI();
                if (rootVisualElement != null) sceneView.rootVisualElement.Add(rootVisualElement);
            }

            // call custom logic on the editor
            OnUpdate(sceneView);
            
            UpdatePopupPosition(screenPoint);
        }
        
        private void UpdatePopupPosition(Vector2 screenPoint)
        {
            if (rootVisualElement == null) return;

            rootVisualElement.style.position = Position.Absolute;
            rootVisualElement.style.left = screenPoint.x - (rootVisualElement.resolvedStyle.width / 2f);
            rootVisualElement.style.top = screenPoint.y - yOffset;
        }

        /// <summary>
        /// Implement this to provide your custom UI Toolkit panel.
        ///
        /// Here you should instance your <see cref="rootVisualElement"/> class and
        /// hook any logic with <see cref="TargetGizmo"/>
        /// </summary>
        protected abstract VisualElement CreateInspectorUI();

        /// <summary>
        /// Implement to provide your own unique logic within the editor, using the
        /// <see cref="TargetGizmo"/> or the <see cref="rootVisualElement"/>
        /// </summary>
        /// <param name="sceneView">editor scene info</param>
        protected abstract void OnUpdate(SceneView sceneView);
    }
}
