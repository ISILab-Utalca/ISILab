using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Core.Settings;
using System;
using System.Collections.Generic;
using UnityEngine;


[DisallowMultipleComponent]
[Serializable]
public abstract class QuestTrigger : MonoBehaviour
{
    #region FIELDS

    [SerializeField]
    protected QuestState state;

    [SerializeField]
    private List<QuestTrigger> previous = new();

    [SerializeField]
    private List<QuestTrigger> next = new();

    #endregion

    #region ACTIONS

    public event Action<QuestTrigger> OnTriggerCompleted;

    #endregion

    #region PROPERTIES

    public QuestState State { get => state; set => state = value; }

    /// <summary>
    /// Gets or sets the next trigger in the sequence. 
    /// Automatically manages the bi-directional pairing safely.
    /// </summary>
    public List<QuestTrigger> Next
    {
        get => next;
    }

    /// <summary>
    /// Read-only access to the previous triggers to prevent external bypassing of validation rules.
    /// </summary>
    public IReadOnlyList<QuestTrigger> Previous => previous;

    #endregion



    #region METHODS

    // Used by generator 3d
    public abstract void InitTrigger(GraphNode paramNode, LBSGenerator3DSettings settings = null, float pivotY = 0);

    public bool TryComplete()
    {
        if (isActiveAndEnabled && CanComplete())
        {
            Complete();
            return true;
        }

        return false;
    }

    protected virtual void Complete()
    {
        state = QuestState.Completed;
        gameObject.SetActive(false);
        OnTriggerCompleted?.Invoke(this);
    }

    // nodes should have their own check, AND & Or trigger branches check that all their previous are true
    protected abstract bool CanComplete();


    public void AddNext(QuestTrigger nextTrigger)
    {
        if (nextTrigger == null || nextTrigger == this) return;
        if (!next.Contains(nextTrigger))
        {
            next.Add(nextTrigger);
            // Ensure the bi-directional link is maintained
            nextTrigger.AddPrevious(this); 
        }
    }
    /// <summary>
    /// Safely registers a previous dependency without creating duplicate references.
    /// </summary>
    private void AddPrevious(QuestTrigger previousTrigger)
    {
        if (previousTrigger == null || previousTrigger == this) return;

        if (!previous.Contains(previousTrigger))
        {
            previous.Add(previousTrigger);
        }
    }

    /// <summary>
    /// Safely removes a previous dependency if it exists.
    /// </summary>
    public void RemovePrevious(QuestTrigger previousTrigger)
    {
        if (previousTrigger == null) return;

        if (previous.Contains(previousTrigger))
        {
            previous.Remove(previousTrigger);
        }
    }

    protected void ClearPrevious() => previous.Clear();

    internal virtual void Activate()
    {
        gameObject.SetActive(true);
        State = QuestState.Active;
    }
    #endregion

}