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

[LBSCustomEditor("NewAssistant", typeof(NewAssistant))]
public class NewAssistantEditor : LBSCustomEditor, IAssistantThreadedEditor
{
    // Reference to the LBSAssistant modified by this Editor.
    private NewAssistant assistant;

    private LBSCustomButton exampleButton;
    private LBSCustomTextField exampleField;

    public CancellationToken CancelToken { get; set; }
    public CancellationTokenSource CancellationTokenSource { get; set; }
    public ToolBarMain TaskBar { get; set; }


    public NewAssistantEditor(object target) : base(target)
    {
        assistant = (NewAssistant)target;
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
        exampleButton = new LBSCustomButton() { text = "Run" };
        exampleButton.clicked += Execute;
        this.Add(exampleButton);

        // Example field. The ValueChangedCallback works well even if the assistant changes,
        // so it doesn't need to be set again when the selected Layer changes.
        exampleField = new LBSCustomTextField() { label = assistant.exampleMember };
        exampleField.RegisterValueChangedCallback(val =>
        {
            assistant.exampleMember = val.newValue;
        });
        this.Add(exampleField);

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
        assistant = target as NewAssistant;
        exampleField.label = assistant.exampleMember;
    }

     /// <summary>
     /// Recommended way to run assistants. Using Threads makes it possible
     /// to keep using Unity while the assistant is running.
     /// </summary>
    private void Execute()
    {
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
                assistant.RunAsync(((IAssistantThreadedEditor)this).ReportProgress, CancelToken);

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
        // ↑↑↑

        DrawManager.Instance.RedrawLevel(LBS.loadedLevel.data);
        LBSMainWindow.Instance.layerPanel.SetSelectedLayer(assistant.Schema.OwnerLayer);
        TaskBar.EnableProcess(false);
        assistant.OnTermination = null;
    }
}
