using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor.Windows.ToolBar;
using ISILab.LBS.Plugin.VisualElements.Editor.AssistantThreads;
using LBS;
using LBS.VisualElements;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("BSP Dungeon Generator", typeof(BSPAssistant))]
    public class BSPAssistantEditor : 
        LBSCustomEditor,            // MUST HAVE
        IToolProvider,              // OPTIONAL
        IAssistantThreadedEditor    // RECOMMENDED
    {
        // References
        BSPAssistant assistant; // MUST HAVE

        // Visual Elements
        LBSCustomRectField areaField;
        LBSCustomIntField minPartitionField;
        LBSCustomIntField minRoomField;
        LBSCustomToggle asyncToggle;

        // Constructor (MUST HAVE)
        // It's important that the assistant reference is saved here,
        // and call CreateVisualElement after that.
        public BSPAssistantEditor(object target) : base(target)
        {
            assistant = target as BSPAssistant;             // MUST HAVE
            assistant.changeCallback += SetFieldsInfo;      // RECOMMENDED
            CreateVisualElement();                          // MUST HAVE
        }

        // CreateVisualElement
        protected override VisualElement CreateVisualElement()
        {
            // Create Visual Elements
            //  |
            //  V
            // Set Callbacks / Add Visual Elements to this / Set fields info

            // Create VisualElements
            // Should create a visual element for each field you'll want to tweak,
            // and a button to run the assistant.
            var runButton = new LBSCustomButton() { text = "Run" };

            areaField = new LBSCustomRectField();
            minPartitionField = new LBSCustomIntField();
            minRoomField = new LBSCustomIntField();
            asyncToggle = new LBSCustomToggle() { text = "Async" };

            // Set Callbacks
            // These are important to modify the assistant values and any other
            // instruction you'll need.
            runButton.clicked += Run;
            areaField.RegisterValueChangedCallback(val =>
            {
                assistant.Area = new RectInt()
                {
                    x = (int)val.newValue.x,
                    y = (int)val.newValue.y,
                    width = (int)val.newValue.width,
                    height = (int)val.newValue.height
                };
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
            this.Add(areaField);
            this.Add(minPartitionField);
            this.Add(minRoomField);
            this.Add(asyncToggle);

            // Set the current variables in the fields
            // Otherwise, they'll display the default (and probably incorrect)
            // values. You must gather the variables from the Assistant reference.
            SetFieldsInfo();
            return this;
        }

        // SetInfo  (MUST HAVE)
        // Called when the selected Layer is changed. Saves the reference
        // of the new Layer, and updates the fields after that.
        public override void SetInfo(object paramTarget)
        {
            assistant = paramTarget as BSPAssistant;    // MUST HAVE
            SetFieldsInfo();                            // RECOMMENDED
        }

        // Updates the Visual Elements display when the assistant values change (RECOMMENDED)
        private void SetFieldsInfo()
        {
            // Size
            areaField.label = "Size";
            areaField.value = new Rect(assistant.Area.x, assistant.Area.y, assistant.Area.width, assistant.Area.height);

            // Min Partition Size
            minPartitionField.label = "Minimum Partition Size";
            minPartitionField.value = assistant.minPartitionSize;

            // Min Room Size
            minRoomField.label = "Minimum Room Size";
            minRoomField.value = assistant.minRoomSize;
        }

        // Sets a Manipulator tool  (OPTIONAL)
        public void SetTools(ToolKit toolKit)
        {
            var manipulator = new BSPAssistantManipulator();
            var t1 = new LBSTool(manipulator);
            t1.OnSelect += LBSInspectorPanel.ActivateAssistantTab;
            toolKit.ActivateTool(t1, assistant.OwnerLayer, assistant);
        }

        // Multi-Thread Stuff
        public CancellationToken CancelToken { get; set; }                      // MUST HAVE
        public CancellationTokenSource CancellationTokenSource { get; set; }    // MUST HAVE
        public ToolBarMain TaskBar { get; set; }                                // MUST HAVE

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

        // Prepares and runs the algorithm in a thread  (HIGHLY RECOMMENDED)
        private void Execute()
        {
            // INIT (RECOMMENDED)
            // Some methods don't work in threads
            string insideStyle = assistant.Schema.PressetInsideStyle.name;
            string outsideStyle = assistant.Schema.PressetOutsideStyle.name;

            // UNDO (RECOMMENDED)
            // Save history version to revert if necessary
            LoadedLevel x = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(x, "Execute BSPDungeon");

            // RUN TASK (MUST HAVE)
            ((IAssistantThreadedEditor)this).SetUpTask(this, assistant);
            Task.Run(() =>
            {
                try
                {
                    assistant.RunAsync(insideStyle, outsideStyle,
                        ((IAssistantThreadedEditor)this).ReportProgress, CancelToken);
                    EditorApplication.delayCall += () => assistant.OnTermination.Invoke("BSPDungeon Generated", LogType.Log, LBSController.CurrentLevel);
                }
                catch (Exception ex)
                {
                    ((IAssistantThreadedEditor)this).OnTaskException(ex, assistant);
                    Debug.LogError("[BSPDungeonAssistantEditor]: " + ex.Message);
                }
            }, CancelToken);
        }

        // This runs when the Task finished running (MUST HAVE IF WORKING WITH THREADS)

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
    }
}
