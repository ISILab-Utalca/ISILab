using System;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Data;
using Unity.Collections;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace ISILab.LBS
{

    [DisallowMultipleComponent]
    [Serializable]
    public class QuestTrigger : MonoBehaviour
    {

        #region FIELDS

        [SerializeField][SerializeReference][HideInInspector] 
        protected QuestNode node;
        
        [SerializeField, ReadOnly] 
        private string nodeID;
        
        protected BoxCollider BoxCollider;
        
        [SerializeField]
        protected bool isCompleted;
       
        private List<GraphNode> _destinations = new();

        #endregion


        #region PROPERTIES

        public string NodeID => nodeID;
        public bool IsCompleted
        {
            get => isCompleted;
            set => isCompleted = value;
        }
        
        public QuestNode Node
        {
            get => node;
            set => node = value;
        }

        public GraphNode OwnerBranchNode { get; set; }
        public List<GraphNode> Destinations { get => _destinations; set => _destinations = value; }

        #endregion


        #region EVENTS

        public event Action<QuestTrigger> OnTriggerCompleted;

        [SerializeField, SerializeReference]
        public UnityEvent onCompleteEvent = new();

        #endregion


        #region INITIALIZATION

        protected void EnsureCollider()
        {
            if (BoxCollider != null) return;
            BoxCollider = GetComponent<BoxCollider>();
            if (BoxCollider != null) return;
            BoxCollider = gameObject.AddComponent<BoxCollider>();
            BoxCollider.isTrigger = true;
            BoxCollider.size = Vector3.one;
        }

        /// <summary>
        /// Call to set SetTypedData from Runtime Function
        /// </summary>
        public virtual void Init()
        {
            EnsureCollider();
        }

        #endregion


        #region DATA SETUP

        /// <summary>
        /// Replace and cast the incoming parameter to the required data type
        /// </summary>
        public virtual void SetUniqueData(QuestActionData data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Always call base from overwrites as base sets the ID that quest observer uses on start 
        /// </summary>
        public void SetData(QuestNode paramNode)
        {
            node = paramNode;
            nodeID = paramNode.ID;

            foreach (UnityActionStored entry in paramNode.Data.EventHooker.RegisteredActions)
            {
                UnityAction completeAction = entry.MakeAction(paramNode.Data.EventHooker.Target);
                #if UNITY_EDITOR
                if (completeAction != null)
                {
                    UnityEventTools.AddPersistentListener(onCompleteEvent, completeAction);
                }
                #else
                onCompleteEvent.AddListener(entry.MakeAction(paramNode.Data.Target));
                #endif
            }
        }

        /// <summary>
        /// All triggers require a size by initialization.
        /// </summary>
        public void SetSize(Vector3 size)
        {
            size.x = Mathf.Abs(size.x);
            size.y = Mathf.Abs(size.y);
            size.z = Mathf.Abs(size.z);
            
            BoxCollider = gameObject.AddComponent<BoxCollider>();
            BoxCollider.isTrigger = true;
            BoxCollider.size = size;
        }

        #endregion


        #region TRIGGER HANDLING

        protected virtual void OnTriggerEnter(Collider other)
        {
            CheckComplete();
        }

        public static bool IsPlayer(Collider other)  { return other.CompareTag("Player"); }
        protected static bool IsPlayer(GameObject other)  { return other.CompareTag("Player"); }

        /// <summary>
        /// TRUE by default. Implement your own complete conditions
        /// </summary>
        protected virtual bool CanComplete()
        {
            return true; 
        }

        public void CheckComplete()
        {
            if (!isActiveAndEnabled) return;
            if (!CanComplete()) return;
            Completed();
        }

        private void Completed()
        {
            isCompleted = true;
            onCompleteEvent?.Invoke();
            
            if (node is not null) 
                node.QuestState = QuestState.Completed;

            gameObject.SetActive(false);
            OnTriggerCompleted?.Invoke(this);
        }

        #endregion


        #region EDITOR

#if UNITY_EDITOR
        /// <summary>
        /// Right click the cog icon in the inspector of the Script
        /// </summary>
        [ContextMenu("Force Complete")]
        private void ForceComplete()
        {
            Completed();
        }
#endif

        #endregion
    }


    #region GENERIC OBJECTIVE TRIGGER

    /// <summary>
    /// Generic class to add box collider to a gameObject.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class GenericObjectiveTrigger : MonoBehaviour
    {
        private QuestTrigger _questTrigger;
        private const float SizeFactor = 1f;

        public void Setup(QuestTrigger trigger)
        {
            _questTrigger = trigger;
    
            BoxCollider boxCollider = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();
    
            boxCollider.isTrigger = true;
            boxCollider.size = Vector3.one * SizeFactor; 
            boxCollider.center = Vector3.zero;
        }
    
        protected void OnTriggerEnter(Collider other)
        {
            if (QuestTrigger.IsPlayer(other)) 
                _questTrigger.CheckComplete();
        }
    }

    #endregion

}
