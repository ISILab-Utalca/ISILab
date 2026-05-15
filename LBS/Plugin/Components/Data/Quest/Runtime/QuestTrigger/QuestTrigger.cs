using ISILab.AI.Grammar;
using ISILab.LBS.Components;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{

    [DisallowMultipleComponent]
    [Serializable]
    public abstract class QuestTrigger : MonoBehaviour
    {

        #region FIELDS

        [SerializeField][SerializeReference][HideInInspector] 
        protected QuestNode node;
        
        [SerializeField, Commons.Attributes.ReadOnly] 
        private string nodeID;
        
        protected BoxCollider BoxCollider;
        
        [SerializeField]
        protected QuestState state;

        [SerializeField]
        private List<GameObject> gos = new();
        [SerializeField]
        private List<GraphNode> _destinations = new();

        private LBSGeneratedEventHook eventHooker;

        [ISILab.Commons.Attributes.ReadOnly]
        [SerializeField]
        private GrammarTerminal _terminal;

        #endregion


        #region PROPERTIES

        public string NodeID => nodeID;

        public QuestState State
        {
            get => state;
            set => state = value;
        }
        
        public QuestNode Node
        {
            get => node;
            set => node = value;
        }
        public GrammarTerminal Terminal
        {
            get => _terminal;
            set => _terminal = value;
        }

        public GraphNode OwnerBranchNode { get; set; }
        public List<GraphNode> Destinations { get => _destinations; set => _destinations = value; }

        #endregion


        #region EVENTS

        public event Action<QuestTrigger> OnTriggerCompleted;

        #endregion


        #region METHODS

        private void Awake()
        {
            eventHooker ??= gameObject.AddComponent<LBSGeneratedEventHook>();
        }

        public void AddGo(GameObject go) => gos.Add(go);
        public void RemoveGo(GameObject go)
        {
            if (gos.Contains(go))
            {
                gos.Remove(go);
            }
        }

        #region SET UP

        /// <summary>
        /// Call to set SetTypedData from Runtime Function
        /// </summary>
        public virtual void Init() => EnsureCollider();

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
        /// Always call base from overwrites as base sets the ID that quest observer uses on start 
        /// </summary>
        public void SetNode(QuestNode paramNode)
        {
            node = paramNode;
            nodeID = paramNode.ID;
            eventHooker ??= gameObject.AddComponent<LBSGeneratedEventHook>();
            eventHooker.AssignEvents(paramNode.Data.EventHooker);

            Terminal = node.Data.Terminal;
            BindFields(node.Data.Fields);

        }

        /// <summary>
        /// Replace and cast the incoming parameter to the required data type
        /// </summary>
        protected virtual void BindFields(List<GrammarField> fields) => throw new NotImplementedException();


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
            if(IsPlayer(other)) TryComplete();
        }

        public static bool IsPlayer(Collider other) { return other.CompareTag("Player"); }

        /// <summary>
        /// TRUE by default. Implement your own complete conditions
        /// </summary>
        protected abstract bool CanComplete();

        /// <summary>
        /// Checks if the trigger can be completed. Returns true if it is completed successfully
        /// </summary>
        public bool TryComplete()
        {
            bool canComplete = CanComplete();
            if (!isActiveAndEnabled || !canComplete) return false;
        
            Complete();
            return true;
        }

        private void Complete()
        {
            // flag to completed
            State = QuestState.Completed;

            // call any events on the event hooker
            if (eventHooker != null) 
                eventHooker.BroadcastEvent(Components.Data.LBSEventType.Complete);

            gameObject.SetActive(false);
            OnTriggerCompleted?.Invoke(this);
        }

        #endregion


#if UNITY_EDITOR
        /// <summary>
        /// Right click the cog icon in the inspector of the Script
        /// </summary>
        [ContextMenu("Force Complete")]
        private void ForceComplete() => Complete();
#endif

    }

    #endregion
}
