using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [Serializable]
    public class QuestTracker : MonoBehaviour
    {
        #region FIELDS

        [SerializeField, SerializeReference] 
        private Graph questGraph;

        #endregion

        #region ACTIONS

        [SerializeField] 
        private UnityEvent onQuestCompleteEvent;

        public event Action OnQuestAdvance;

        #endregion

        #region PROPERTIES

        public QuestTrigger CurrentQuest { get; private set; }
        public Graph QuestGraph { get => questGraph; }

        public List<QuestTrigger> ActiveTriggers
        {
            get
            {
                List<QuestTrigger> actives = new();
                foreach(var trigger in Triggers)
                {
                    if (trigger.State == QuestState.Active)
                        actives.Add(trigger);
                }
                return actives;
            }
        }

        public List<QuestTrigger> Triggers
        {
            get
            {
                List<QuestTrigger> triggers = new();
                triggers = GetComponentsInChildren<QuestTrigger>(true).ToList();
                return triggers;
            }
        }

        #endregion

        #region METHODS
        private void Awake() => StartQuest();

        public void Init(Graph graph) => questGraph = graph;

        private void StartQuest()
        {
            if (questGraph?.Root == null) return;

            foreach (var trigger in Triggers)
            {
                // Sync runtime state to graph and initialize trigger
                //trigger.InitTrigger(node);
                //node.QuestState = QuestState.Blocked;
                trigger.State = QuestState.Blocked;
                trigger.gameObject.SetActive(false);

                trigger.OnTriggerCompleted += HandleTriggerCompletion;

                if(trigger is QuestTriggerNode qtn)
                {
                    if (qtn.NodeType == QuestNode.NodeGraphType.Start)
                    {
                        ActivateNode(qtn);
                    }
                }
            }

        }

        private void HandleTriggerCompletion(QuestTrigger trigger)
        {
            // check if quest shuold end
            if (trigger is QuestTriggerNode qtn)
            {
                if (qtn.NodeType == QuestNode.NodeGraphType.Goal)
                {
                    CompleteWholeQuest();
                    return;
                }
            }

            // keep progressing, activating next
            foreach (var next in trigger.Next)
                ActivateNode(next);
            OnQuestAdvance?.Invoke();
        }

        private void ActivateNode(QuestTrigger trigger)
        {
            if (trigger == null)
                return;

            trigger.Activate();
            CurrentQuest = trigger;
        }

        private void CompleteWholeQuest()
        {
            onQuestCompleteEvent?.Invoke();
            OnQuestAdvance?.Invoke();
            Debug.Log("<color=green>Quest Narrative Complete!</color>");
        }

        
        #endregion
    }
}