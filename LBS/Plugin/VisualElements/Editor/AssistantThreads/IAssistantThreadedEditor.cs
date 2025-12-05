using System;
using System.Threading;
using ISILab.LBS.Assistants;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.VisualElements.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public interface IAssistantThreadedEditor
{
    CancellationToken CancelToken { get; set; }
    CancellationTokenSource CancellationTokenSource { get; set; }
    ToolBarMain TaskBar  { get; set; }
    
    // recommended to encapsulate logic within an "EditorApplication.delayCall"
    protected abstract void OnAssistantTermination(string log = "", LogType type = LogType.Log);
    

    public void OnTaskException(Exception ex, LBSAssistant Assistant)
    {
        EditorApplication.delayCall += () =>
        {
            Debug.LogError($"{Assistant.Name} Task failed: {ex}");
            TaskBar.EnableProcess(false);
        };
    }
    
    public void CancelCurrentTask()
    {
        if(CancellationTokenSource == null) return;
        if(CancellationTokenSource.IsCancellationRequested) return;
        CancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Will hook up all the different actions and functions for the cancellation, progress update and
    /// termination of a threaded task from an assistant.
    /// </summary>
    /// <remarks>
    /// Use the method like this:
    /// <code>
    ///         ((IAssistantThreadedEditor)this).SetUpTask(this, assistant);
    /// </code>
    /// Before the Task runs
    ///
    /// Make sure your assistant calls <see cref="LBSAssistant.OnTermination"/> once it finishes
    /// </remarks>
    /// 
    /// <param name="InterfaceOwner">object that implements the interface from which the task is called</param>
    /// <param name="Assistant">The <see cref="LBSAssistant"/> whose method will be called within the task</param>
    public void SetUpTask(object InterfaceOwner, LBSAssistant Assistant)
    {
        IAssistantThreadedEditor IATE = (IAssistantThreadedEditor)InterfaceOwner;
        
        // cancel old token if exists
        IATE.CancellationTokenSource?.Cancel();
        
        // make new token
        IATE.CancellationTokenSource = new CancellationTokenSource();
        IATE.CancelToken = CancellationTokenSource.Token;

        // assign if null
        IATE.TaskBar ??= LBSMainWindow.Instance.rootVisualElement.Q<ToolBarMain>();
            
        // update cancel hook
        IATE.TaskBar.OnProgressCancelled -= CancelCurrentTask;
        IATE.TaskBar.OnProgressCancelled += CancelCurrentTask;

        Assistant.OnTermination = null;
        Assistant.OnTermination -= HandleTermination;
        Assistant.OnTermination += HandleTermination;

        IATE.TaskBar.EnableProcess(true, Assistant.Name);

        Debug.Log($"{Assistant.Name} Task started.");
    }
    
    private void HandleTermination(string log, LogType type)
    {
        EditorApplication.delayCall += () =>
        {
            OnAssistantTermination(log, type);
        };
    }

    public void ReportProgress(float normalized)
    {
        void UpdateOnce()
        {
            TaskBar.SetProgressPercent(normalized);
            EditorApplication.update -= UpdateOnce;
        }

        EditorApplication.update += UpdateOnce;
    }

}
