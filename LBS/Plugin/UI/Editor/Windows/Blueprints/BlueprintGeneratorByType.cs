using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.Settings;
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
                        var target = FindMergeTargetByType(existingLayers, layer);

                        if (target != null)
                            if (target.MergeLayerData(layer, overwrite))
                            {
                                modifiedLayers.Add(layer);
                            }
                            else
                            {
                                LBSMainWindow.Instance.layerPanel.AddLayer(layer);
                            }
                        else
                        {
                            LBSLog log = new LBSLog(
                                $"Failed to find >{layer.ID}< Type Layer. Can't add blueprint layer",
                                LogType.Warning,
                                4);
                            LBSMainWindow.MessageNotify(log);
                        }
                    };

                    onProgress?.Invoke((float)i / generatedLayers.Count);
                    Thread.Sleep(1);
                }
            }

            return modifiedLayers;
        }
    }
}