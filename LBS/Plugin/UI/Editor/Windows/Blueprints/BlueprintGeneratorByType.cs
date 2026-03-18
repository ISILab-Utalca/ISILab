using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint
{
    public class BlueprintGeneratorByType : BlueprintGenerator
    {
        public BlueprintGeneratorByType(
            string IconGuid = "",
            string name = "",
            Color colorTint = new Color(),
            Action onStart = null) : base(IconGuid, name, colorTint, onStart)
        {
        }

        public override object Clone() => throw new NotImplementedException();
        public override void OnGUI() => throw new NotImplementedException();

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
                    var target = FindMergeTargetByType(existingLayers, layer);

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