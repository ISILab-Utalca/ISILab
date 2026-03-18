
using ISILab.LBS.Editor.Windows;
using System;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint
{
    public class BlueprintGeneratorByName : BlueprintGenerator
    {
        public BlueprintGeneratorByName(
            string IconGuid = "",
            string name = "",
            Color colorTint = new Color(),
            Action onStart = null) : base(IconGuid, name, colorTint, onStart)
        {
        }

        public override object Clone()
        {
            return null;
        }
        public override void OnGUI()
        {

        }
        public override void Generate(Action<float> onProgress = null, CancellationToken token = default)
        {
            var window = LBSMainWindow.Instance;
            if (window == null) return;

            var existingLayers = window.GetLayers();

            for (int i = 0; i < generatedLayers.Count; i++)
            {
                var layer = generatedLayers[i];

                EditorApplication.delayCall += () =>
                {
                    var target = FindMergeTargetByName(existingLayers, layer);

                    if (target != null)
                        target.MergeLayerData(layer, overwrite);
                    else
                        window.layerPanel.AddLayer(layer);
                };

                onProgress?.Invoke((float)i / generatedLayers.Count);
                Thread.Sleep(1);
            }
        }
    }
}