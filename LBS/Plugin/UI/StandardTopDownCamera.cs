using UnityEditor;
using UnityEngine;

namespace ISILab.Commons
{
    public class StandardTopDownCamera
    {
        private const float PADDING = 1.2f;

        public static void SetStandardTopDown(GameObject target)
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null) return;

            Vector3 centerPoint = Vector3.zero;
            float cameraSize = 10f;

            if (target != null)
            {
                Bounds bounds = CalculateBounds(target);

                centerPoint = bounds.center;

                float maxDimension = Mathf.Max(bounds.size.x, bounds.size.z);

                cameraSize = (maxDimension / 2f) * PADDING;

                cameraSize = Mathf.Max(cameraSize, 1f);
            }

            Quaternion standardRotation = Quaternion.Euler(60f, 0f, 0f);
            view.orthographic = false;

            view.LookAt(centerPoint, standardRotation, cameraSize);
            view.Repaint();
        }

        private static Bounds CalculateBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                return new Bounds(obj.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;

            foreach (Renderer r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            return bounds;
        }
    }
}