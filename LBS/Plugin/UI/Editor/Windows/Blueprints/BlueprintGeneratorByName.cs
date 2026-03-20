
using ISILab.LBS.Editor.Windows;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public override List<LBSLayer> Generate(Action<float> onProgress = null, CancellationToken token = default)
        {
            List<LBSLayer> modifiedLayers = new();
            if(LBSMainWindow.Instance != null)
            {
                List<LBSLayer> existingLayers = new List<LBSLayer>(LBSMainWindow.Instance.GetLayers());
                existingLayers.Reverse();

                for (int i = 0; i < generatedLayers.Count; i++)
                {
                    var layer = generatedLayers[i];

                    EditorApplication.delayCall += () =>
                    {
                        var target = FindMergeTargetByName(existingLayers, layer);

                        if (target != null)
                            if (target.MergeLayerData(layer, overwrite))
                            {
                                modifiedLayers.Add(layer);
                            }
                            else
                                LBSMainWindow.Instance.layerPanel.AddLayer(layer);
                    };

                    onProgress?.Invoke((float)i / generatedLayers.Count);
                    Thread.Sleep(1);
                }
            }

            return modifiedLayers;
        }
    }
}