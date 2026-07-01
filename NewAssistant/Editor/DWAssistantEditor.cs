using ISILab.LBS;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor.Windows.ToolBar;
using ISILab.LBS.Plugin.VisualElements.Editor.AssistantThreads;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[LBSCustomEditor("DWAssistant", typeof(DWAssistant))]
public class DWAssistantEditor : LBSCustomEditor, IAssistantThreadedEditor
{
    // Reference to the LBSAssistant modified by this Editor.
    private DWAssistant assistant;

    private LBSCustomButton runButton;
    private LBSCustomRectField areaField;
    private LBSCustomIntField maxRoomsField;
    private LBSCustomIntField distanceField;
    private LBSCustomVector2IntField minSizeField;
    private LBSCustomVector2IntField maxSizeField;

    public CancellationToken CancelToken { get; set; }
    public CancellationTokenSource CancellationTokenSource { get; set; }
    public ToolBarMain TaskBar { get; set; }


    public DWAssistantEditor(object target) : base(target)
    {
        assistant = (DWAssistant)target;
        CreateVisualElement();
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
        runButton = new LBSCustomButton() { text = "Run" };
        runButton.clicked += Execute;
        this.Add(runButton);

        // Area field.
        areaField = new LBSCustomRectField() { label = "Map Size" };
        areaField.RegisterValueChangedCallback(val =>
        {
            assistant.area = new RectInt()
            {
                x = (int)val.newValue.x,
                y = (int)val.newValue.y,
                width = (int)val.newValue.width,
                height = (int)val.newValue.height
            };
        });
        this.Add(areaField);

        // Max rooms field.
        maxRoomsField = new LBSCustomIntField() { label = "Max Rooms" };
        maxRoomsField.RegisterValueChangedCallback(val =>
        {
            assistant.totalRooms = val.newValue;
        });
        this.Add(maxRoomsField);

        // Distance field.
        distanceField = new LBSCustomIntField() { label = "Distance" };
        distanceField.RegisterValueChangedCallback(val =>
        {
            assistant.walkDistanceBetweenRooms = val.newValue;
        });
        this.Add(distanceField);

        // Min Size field.
        minSizeField = new LBSCustomVector2IntField() { label = "Min Size" };
        minSizeField.RegisterValueChangedCallback(val =>
        {
            assistant.minRoomSize = val.newValue;
        });
        this.Add(minSizeField);

        // Max Size field.
        maxSizeField = new LBSCustomVector2IntField() { label = "Max Size" };
        maxSizeField.RegisterValueChangedCallback(val =>
        {
            assistant.maxRoomSize = val.newValue;
        });
        this.Add(maxSizeField);

        // Set initial info
        SetFieldsInfo();
        return this;
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
        assistant = target as DWAssistant;
        SetFieldsInfo();
    }

    private void SetFieldsInfo()
    {
        areaField.value = new Rect(assistant.area.x, assistant.area.y, assistant.area.width, assistant.area.height);
        maxRoomsField.value = assistant.totalRooms;
        distanceField.value = assistant.walkDistanceBetweenRooms;
        minSizeField.value = assistant.minRoomSize;
        maxSizeField.value = assistant.maxRoomSize;
    }

     /// <summary>
     /// Recommended way to run assistants. Using Threads makes it possible
     /// to keep using Unity while the assistant is running.
     /// </summary>
    private void Execute()
    {
        // Init
        string insideStyle = assistant.Schema.PressetInsideStyle.name;
        string outsideStyle = assistant.Schema.PressetOutsideStyle.name;

        // Save history version to revert if necessary
        LoadedLevel x = LBSController.CurrentLevel;
        EditorGUI.BeginChangeCheck();
        Undo.RegisterCompleteObjectUndo(x, "Execute DWAssistant");

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
                () => assistant.OnTermination.Invoke("DWAssistant Generated", LogType.Log, LBSController.CurrentLevel);
            }
            // Catches any error that might come. It's necessary to explicitly display the error,
            // since Thread errors aren't displayed on the UNity console by default.
            catch (Exception ex)
            {
                ((IAssistantThreadedEditor)this).OnTaskException(ex, assistant);
                Debug.LogError("[DWAssistantEditor]: " + ex.Message);
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
        LBSMainWindow.Instance.layerPanel.SetSelectedLayer(assistant.OwnerLayer);

        TaskBar.EnableProcess(false);
        assistant.OnTermination = null;
    }
}
