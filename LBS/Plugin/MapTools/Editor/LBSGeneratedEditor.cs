using UnityEditor;
using UnityEngine;
using ISILab.LBS.Plugin.MapTools.Generators;
using ISILab.LBS.Plugin.Components.Bundles;


namespace ISILab.LBS.Plugin.MapTools.Editor
{
    [CustomEditor(typeof(LBSGenerated))]   
    public class LBSGeneratedEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            LBSGenerated LBSgen = (LBSGenerated)target;

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Original Bundle", LBSgen.BundleRef, typeof(Bundle), false);
            EditorGUILayout.ObjectField("Temporal Bundle", LBSgen.BundleTemp, typeof(Bundle), false);
        }
    }
}
