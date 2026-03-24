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
        protected List<LBSLayer> modifiedLayers = new();
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

            var Level = (LoadedLevel)loadedLevel;
            if (!overwrite)
                DrawManager.Instance.RedrawLevel(Level.data);
            else
            {
                foreach (var modlayer in modifiedLayers)
                    DrawManager.Instance.UpdateLayer(modlayer);
            }


            TaskBar.EnableProcess(false);
            OnTermination =null;
        }
        #endregion

        // should return the list of modified layers so they are updated by the drawer manager
        abstract public List<LBSLayer> Generate(Action<float> onProgress = null, CancellationToken token = default);

        public void CreateBlueprint(List<LBSLayer> layersToPrint, LoadedLevel loadedLevel, bool overwrite)
        {
            if (loadedLevel == null || LBSMainWindow.Instance == null) return;
            this.overwrite = overwrite;

            modifiedLayers.Clear();
            generatedLayers.Clear();

            for(int i = 0; i < layersToPrint.Count;i++)
            {
                generatedLayers.Add(layersToPrint[i].Clone() as LBSLayer);
            }

            ((IAssistantThreadedEditor)this).SetUpTask(this, this);
            Task.Run(() =>
            {
                try
                {
                    modifiedLayers = Generate(((IAssistantThreadedEditor)this).ReportProgress, CancelToken);
                    EditorApplication.delayCall += () =>
                    {
                        OnTermination.Invoke("Blueprint Generated", LogType.Log, loadedLevel);

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
