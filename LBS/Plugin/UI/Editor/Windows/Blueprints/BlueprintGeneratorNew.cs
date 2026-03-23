
using ISILab.LBS.Editor.Windows;
using LBS.Components;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint
{
    public class BlueprintGeneratorNew : BlueprintGenerator
    {
        public BlueprintGeneratorNew(
            string IconGuid = "",
            string name = "",
            Color colorTint = new Color(),
            Action onStart = null) : base(IconGuid, name, colorTint, onStart)
        {
        }

        public override object Clone() => throw new NotImplementedException();
        public override void OnGUI() => throw new NotImplementedException();

        public override List<LBSLayer> Generate(Action<float> onProgress = null, CancellationToken token = default)
        {
            List<LBSLayer> modifiedLayer = new();
            if (LBSMainWindow.Instance != null)
            {

                for (var index = 0; index < generatedLayers.Count; index++)
                {
                    var layer = generatedLayers[index];
                    EditorApplication.delayCall += () =>
                    {
                        if (LBSMainWindow.Instance != null)
                            LBSMainWindow.Instance.layerPanel.AddLayer(layer);
                    };

                    onProgress?.Invoke((float)index / generatedLayers.Count);
                    Thread.Sleep(1); // to draw
                }
            }

            return modifiedLayer;
        }

    }
}