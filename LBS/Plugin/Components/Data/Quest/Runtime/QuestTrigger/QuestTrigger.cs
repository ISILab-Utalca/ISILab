using ISILab.AI.Grammar;
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
    private List<QuestTrigger> allPrevious = new();

    [SerializeField]
    private QuestTrigger next;

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
    public QuestTrigger Next
    {
        get => next;
        set
        {
            // If it's already assigned to this value, do nothing
            if (next == value) return;

            // Optional: If overwriting an old next node, clear this trigger from its previous list
            if (next != null)
            {
                next.RemovePrevious(this);
            }

            next = value;

            // Automatically register this trigger into the new next node's previous tracking
            if (next != null)
            {
                next.AddPrevious(this);
            }
        }
    }

    /// <summary>
    /// Read-only access to the previous triggers to prevent external bypassing of validation rules.
    /// </summary>
    public IReadOnlyList<QuestTrigger> AllPrevious => allPrevious;

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

    /// <summary>
    /// Safely registers a previous dependency without creating duplicate references.
    /// </summary>
    public void AddPrevious(QuestTrigger previousTrigger)
    {
        if (previousTrigger == null || previousTrigger == this) return;

        if (!allPrevious.Contains(previousTrigger))
        {
            allPrevious.Add(previousTrigger);
        }
    }

    /// <summary>
    /// Safely removes a previous dependency if it exists.
    /// </summary>
    public void RemovePrevious(QuestTrigger previousTrigger)
    {
        if (previousTrigger == null) return;

        if (allPrevious.Contains(previousTrigger))
        {
            allPrevious.Remove(previousTrigger);
        }
    }
    #endregion
}