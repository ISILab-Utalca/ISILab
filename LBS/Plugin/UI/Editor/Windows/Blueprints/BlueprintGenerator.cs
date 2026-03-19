using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor.Windows.ToolBar;
using ISILab.LBS.Plugin.VisualElements.Editor.AssistantThreads;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint
{
    public abstract class BlueprintGenerator : LBSAssistant, IAssistantThreadedEditor
    {
        protected List<LBSLayer> generatedLayers = new();
        protected bool overwrite = false;
        protected BlueprintGenerator(
            string IconGuid ="", 
            string name="", 
            Color colorTint = new Color(), 
            Action onStart = null) : base(IconGuid, name, colorTint, onStart)
        {
        }

        #region IAssistantThreadedEditor
        public CancellationToken CancelToken { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public ToolBarMain TaskBar { get; set; }

        void IAssistantThreadedEditor.OnAssistantTermination(string log, LogType type, UnityEngine.Object loadedLevel)
        {
            LBSMainWindow.MessageNotify(new LBSLog(log, type));

            // Mark as dirty
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(loadedLevel);
            }
            foreach(var layer in generatedLayers)
            {
                DrawManager.Instance.RedrawLayer(layer);
            }


            TaskBar.EnableProcess(false);
            OnTermination = null;
        }
        #endregion

        abstract public void Generate(Action<float> onProgress = null, CancellationToken token = default);

        public void CreateBlueprint(List<LBSLayer> layersToPrint, LoadedLevel loadedLevel, bool overwrite)
        {
            if (loadedLevel == null) return;
            this.overwrite = overwrite;

            generatedLayers = layersToPrint;
            ((IAssistantThreadedEditor)this).SetUpTask(this, this);
            Task.Run(() =>
            {
                try
                {
                    Generate(((IAssistantThreadedEditor)this).ReportProgress, CancelToken);
                    EditorApplication.delayCall += () =>
                    {
                        OnTermination.Invoke("Blueprint Generated", LogType.Log, loadedLevel);
                        foreach (var existingLayer in LBSMainWindow.Instance.GetLayers())
                            DrawManager.Instance.UpdateLayer(existingLayer);
                    };
                }
                catch (Exception ex)
                {
                    ((IAssistantThreadedEditor)this).OnTaskException(ex, this);
                }
            }, CancelToken);
          
        }

        protected LBSLayer FindMergeTargetByType(List<LBSLayer> existing, LBSLayer incoming)
        {
            for (int i = 0; i < existing.Count; i++)
            {
                var layer = existing[i];

                if (layer.ID == incoming.ID)
                    return layer;
            }

            return null;
        }

        protected LBSLayer FindMergeTargetByName(List<LBSLayer> existing, LBSLayer incoming)
        {
            for (int i = 0; i < existing.Count; i++)
            {
                var layer = existing[i];

                if (layer.Name == incoming.Name)
                    return layer;
            }

            return null;
        }
    }
}
