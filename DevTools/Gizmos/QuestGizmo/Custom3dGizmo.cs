using ISILab.LBS.VisualElements;
using UnityEditor;
using UnityEngine;

namespace ISI_Lab.LBS.DevTools
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    public class Custom3dGizmo : MonoBehaviour
    {
        public Color gizmoColor = new(1f, 0.67f, 0.06f);
        public Mesh gizmoMesh;
        [Range(0f,1f)]
        public float meshGizmoScale = 0.3f;
        public Vector3 worldPosition = Vector3.zero;
        
        private MeshRenderer mRendererComponent;
        public PrevStepButton RootVisualElement { get; set; }

 private void OnEnable()
        {
            Selection.selectionChanged += UpdatePosition;
            mRendererComponent = GetComponent<MeshRenderer>();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= UpdatePosition;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr)
            {
                worldPosition = mr.bounds.center;
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireCube(mr.bounds.center, mr.bounds.size);
                
                Gizmos.DrawWireMesh(
                    gizmoMesh,
                    mr.bounds.center,
                    Quaternion.identity,
                    new Vector3(meshGizmoScale,meshGizmoScale,meshGizmoScale)
                );
            }
        }

        public void UpdatePosition()
        {
            if (mRendererComponent)
            {
                worldPosition = mRendererComponent.bounds.center;
            }
        }
    }
}
