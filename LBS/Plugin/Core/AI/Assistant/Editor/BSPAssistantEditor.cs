using ISILab.LBS;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor.Windows.ToolBar;
using ISILab.LBS.Plugin.VisualElements.Editor.AssistantThreads;
using LBS;
using LBS.Components;
using LBS.VisualElements;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("BSPAssistant", typeof(BSPAssistant))]
    public class BSPAssistantEditor : LBSCustomEditor, IAssistantThreadedEditor, IToolProvider
    {
        // Reference to the LBSAssistant modified by this Editor.
        private BSPAssistant assistant;

        // Visual Elements
        private LBSCustomButton exampleButton;
        LBSCustomRectField areaField;
        LBSCustomIntField minPartitionField;
        LBSCustomIntField minRoomField;

        public CancellationToken CancelToken { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public ToolBarMain TaskBar { get; set; }


        public BSPAssistantEditor(object target) : base(target)
        {
            assistant = (BSPAssistant)target;
            assistant.AreaChanged += SetFieldsInfo;
            CreateVisualElement();
        }

        public void SetTools(ToolKit toolKit)
        {
            var manipulator = new NewAssistantManipulator();
            manipulator.Execute += Execute;
            var t1 = new LBSTool(manipulator);
            t1.OnSelect += LBSInspectorPanel.ActivateAssistantTab;
            toolKit.ActivateTool(t1, assistant.OwnerLayer, assistant);
        }

        /// <summary>
        /// Unity's method to build the UI. 
        /// </summary>
        /// <remarks>
        /// This is only called once in the constructor, for updating the Editor when a new 
        /// Layer is selected, use SetInfo.
        /// All your VisualElements must be created here and added to this Editor, who works
        /// as the root for all the assistant UI.
        /// </remarks>
        protected override VisualElement CreateVisualElement()
        {
            // Example button to easily run the assistant
            exampleButton = new LBSCustomButton() { text = "Run" };
            exampleButton.clicked += Execute;
            this.Add(exampleButton);

            areaField = new LBSCustomRectField();
            minPartitionField = new LBSCustomIntField();
            minRoomField = new LBSCustomIntField();

            // Set Callbacks
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

            // Add VisualElements to the Editor
            this.Add(areaField);
            this.Add(minPartitionField);
            this.Add(minRoomField);

            // Set the current variables in the fields
            SetFieldsInfo();
            return this;
        }
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

        /// <summary>
        /// Updates the Editor's internal reference to the target Assistant.
        /// </summary>
        /// <param name="target">New assistant instance.</param>
        /// <remarks>
        /// - Called when a new Layer is selected in the LBS window.
        /// - Be sure to update any VisualElement that displays values from the assistant.
        /// </remarks>
        public override void SetInfo(object target)
        {
            assistant = target as BSPAssistant; 
            SetFieldsInfo();
        }

         /// <summary>
         /// Recommended way to run assistants. Using Threads makes it possible
         /// to keep using Unity while the assistant is running.
         /// </summary>
        private void Execute()
        {
            string insideStyle = assistant.Schema.PressetInsideStyle.name;
            string outsideStyle = assistant.Schema.PressetOutsideStyle.name;

            // Save history version to revert if necessary
            LoadedLevel x = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(x, "Execute NewAssistant");

            // Runs the assistant in a Thread
            ((IAssistantThreadedEditor)this).SetUpTask(this, assistant);
            Task.Run(() =>
            {
                try
                {
                    assistant.RunAsync(insideStyle, outsideStyle,
                        ((IAssistantThreadedEditor)this).ReportProgress, CancelToken);

                    // Invoke the assistant's OnTermination method after it finishes running.
                    EditorApplication.delayCall += 
                    () => assistant.OnTermination.Invoke("NewAssistant Generated", LogType.Log, LBSController.CurrentLevel);
                }
                // Catches any error that might come. It's necessary to explicitly display the error,
                // since Thread errors aren't displayed on the UNity console by default.
                catch (Exception ex)
                {
                    ((IAssistantThreadedEditor)this).OnTaskException(ex, assistant);
                    Debug.LogError("[NewAssistantEditor]: " + ex.Message);
                }
            }, CancelToken);
        }

        /// <summary>
        /// Callback invoked after the assistant finishes running.
        /// </summary>
        /// <param name="log">Message to the user.</param>
        /// <param name="type">Type of log (Info, Warning, Error).</param>
        /// <param name="loadedLevel">Reference to the loaded/affected level (to mark it as dirty).</param>
        void IAssistantThreadedEditor.OnAssistantTermination(string log, LogType type, UnityEngine.Object loadedLevel)
        {
            LBSMainWindow.MessageNotify(new LBSLog(log, type));

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(loadedLevel);
            }

            // If you need to do some action after running the Assistant, this is the best place.
            // ↓↓↓
            assistant.Schema.RecalculateWalls();
            // ↑↑↑

            // Easy way to redraw the layer after the assistant runs, if it modifies it.
            DrawManager.Instance.RedrawLevel(LBS.loadedLevel.data);
            LBSMainWindow.Instance.layerPanel.SetSelectedLayer(assistant.OwnerLayer);

            TaskBar.EnableProcess(false);
            assistant.OnTermination = null;
        }
    }
}

public class NewAssistantManipulator : ManipulateTeselation
{
    private Vector2Int _cornerStart;
    private BSPAssistant assistant;

    protected override string IconGuid => "5ab039ea1b079eb4dbe013d7a618c2aa";

    public event System.Action Execute;

    public NewAssistantManipulator()
    {
        Feedback.fixToTeselation = true;
        Name = "BSP Dungeon Generator";
        Description = "Select an area to generate a dungeon usign the Binary Space Partition algorithm.";
        groupWeight = 5;

    }

    // Manipulator Methods
    public override void Init(LBSLayer layer, object owner)
    {
        base.Init(layer, owner);
        assistant = owner as BSPAssistant;
    }
    protected override void OnMouseDown(VisualElement element, Vector2Int position, MouseDownEvent e)
    {
        _cornerStart = position;
    }

    protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
    {
        base.OnMouseUp(element, endPosition, e);

        //If esc key was pressed, cancel the operation
        if (ForceCancel)
        {
            ForceCancel = false;
            return;
        }

        var corners = assistant.OwnerLayer.ToFixedPosition(_cornerStart, endPosition);
        var mapWidth = corners.max.x - corners.min.x;
        var mapHeight = corners.max.y - corners.min.y;
        assistant.Area = new RectInt(corners.min.x, corners.min.y, mapWidth, mapHeight);
        Execute?.Invoke();
    }
}