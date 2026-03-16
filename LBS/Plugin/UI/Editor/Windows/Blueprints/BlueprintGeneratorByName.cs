
using ISILab.LBS.Editor.Windows;
using LBS.Components;
using System;
using System.Collections.Generic;
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
            throw new NotImplementedException();
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
                        target.Merge(layer, overwrite);
                    else
                        window.layerPanel.AddLayer(layer);
                };

                onProgress?.Invoke((float)i / generatedLayers.Count);
                Thread.Sleep(1);
            }
        }

        public override void OnGUI()
        {
            throw new NotImplementedException();
        }
    }

}