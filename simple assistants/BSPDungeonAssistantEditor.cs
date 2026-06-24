using Codice.Utils;
using ISILab.LBS;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor.Windows.ToolBar;
using ISILab.LBS.Plugin.VisualElements.Editor.AssistantThreads;
using LBS.VisualElements;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("BSP Dungeon Generator", typeof(BSPDungeonAssistant))]
    public class BSPDungeonAssistantEditor : LBSCustomEditor, IToolProvider, IAssistantThreadedEditor
    {
        // References
        BSPDungeonAssistant assistant;

        // Visual Elements
        LBSCustomVector2IntField sizeField;
        LBSCustomIntField minPartitionField;
        LBSCustomIntField minRoomField;
        LBSCustomToggle asyncToggle;

        // Constructor
        // It's important that the assistant reference is saved here,
        // and call CreateVisualElement after that.
        public BSPDungeonAssistantEditor(object target) : base(target)
        {
            assistant = target as BSPDungeonAssistant;
            CreateVisualElement();
        }

        // CreateVisualElement
        protected override VisualElement CreateVisualElement()
        {
            // Create VisualElements
            // Should create a visual element for each field you'll want to tweak,
            // and a button to run the assistant.
            var runButton = new LBSCustomButton() { text = "Run" };
            sizeField = new LBSCustomVector2IntField();
            minPartitionField = new LBSCustomIntField();
            minRoomField = new LBSCustomIntField();
            asyncToggle = new LBSCustomToggle() { text = "Async" };

            // Set Callbacks
            // These are important to modify the assistant values and any other
            // instruction you'll need.
            runButton.clicked += Run;
            sizeField.RegisterValueChangedCallback(val =>
            {
                assistant.mapWidth = val.newValue.x;
                assistant.mapHeight = val.newValue.y;
            });
            minPartitionField.RegisterValueChangedCallback(val =>
            {
                assistant.minPartitionSize = val.newValue;
            });
            minRoomField.RegisterValueChangedCallback(val =>
            {
                assistant.minRoomSize = val.newValue;
            });

            // Add the VisualElements to "this"
            // LBSCustomEditor are displayed on the LBS inspector panel, along
            // with it's children. Easy way to render new VisualElements.
            this.Add(runButton);
            this.Add(sizeField);
            this.Add(minPartitionField);
            this.Add(minRoomField);
            this.Add(asyncToggle);

            // Set the current variables in the fields
            // Otherwise, they'll display the default (and probably incorrect)
            // values. You must gather the variables from the Assistant reference.
            SetFieldsInfo();
            return this;
        }

        // SetInfo
        // Called when the selected Layer is changed. Saves the reference
        // of the new Layer, and updates the fields after that.
        public override void SetInfo(object paramTarget)
        {
            assistant = paramTarget as BSPDungeonAssistant;
            SetFieldsInfo();
        }
        private void SetFieldsInfo()
        {
            // Size
            sizeField.label = "Size";
            sizeField.value = new Vector2Int(assistant.mapWidth, assistant.mapHeight);

            // Min Partition Size
            minPartitionField.label = "Minimum Partition Size";
            minPartitionField.value = assistant.minPartitionSize;

            // Min Room Size
            minRoomField.label = "Minimum Room Size";
            minRoomField.value = assistant.minRoomSize;
        }
        /*
        public override void SetTools()
        {

        }//*/


        // Multi-Thread Stuff
        public CancellationToken CancelToken { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public ToolBarMain TaskBar { get; set; }
        
        private void Run()
        {
            if (asyncToggle.value)
            {
                Execute();
            }
            else
            {
                assistant.RunSynced();
            }
        }

        private void Execute()
        {
            // Save history version to revert if necessary
            LoadedLevel x = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(x, "Execute BSPDungeon");

            ((IAssistantThreadedEditor)this).SetUpTask(this, assistant);
            Task.Run(() =>
            {
                try
                {
                    assistant.RunAsync(((IAssistantThreadedEditor)this).ReportProgress, CancelToken);
                    EditorApplication.delayCall += () => assistant.OnTermination.Invoke("BSPDungeon Generated", LogType.Log, LBSController.CurrentLevel);
                }
                catch (Exception ex)
                {
                    ((IAssistantThreadedEditor)this).OnTaskException(ex, assistant);
                    Debug.LogError("[BSPDungeonAssistantEditor]: " + ex.Message);
                }
            }, CancelToken);
        }

        void IAssistantThreadedEditor.OnAssistantTermination(string log, LogType type, UnityEngine.Object loadedLevel)
        {
            LBSMainWindow.MessageNotify(new LBSLog(log, type));

            // Mark as dirty
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(loadedLevel);
            }

            assistant.Schema.RecalculateWalls();
            DrawManager.Instance.RedrawLevel(LBS.loadedLevel.data);
            LBSMainWindow.Instance.layerPanel.SetSelectedLayer(assistant.Schema.OwnerLayer);

            TaskBar.EnableProcess(false);
            assistant.OnTermination = null;
        }

        public void SetTools(ToolKit toolkit) { }
    }
}
