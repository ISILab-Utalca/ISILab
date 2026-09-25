using ISILab.LBS.Macros;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Plugin.Components.Data
{

    [Serializable]
    public class LBSEventHooker : ISerializationCallbackReceiver, ICloneable
    {
        #region FIELDS
        [SerializeField] private List<UnityActionStored> registeredActions = new();
        [SerializeField] private string targetName = string.Empty;
        [SerializeField] private string sceneGuid = string.Empty;
        [NonSerialized] private GameObject _target;
        #endregion

        #region PROPERTIES
        public List<UnityActionStored> RegisteredActions
        {
            get
            {
                registeredActions ??= new List<UnityActionStored>();
                return registeredActions;
            }
        }

        public GameObject Target
        {
            get
            {
                if (_target is not null) return _target;

                if (LBSAssetMacro.GetActiveSceneGUID() == sceneGuid)
                {
                    Target = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                        .FirstOrDefault(o => o.name == targetName);
                }

                return _target;
            }
            set
            {
                _target = value;
                if(_target is null)
                {
                    registeredActions.Clear();
                    targetName = string.Empty;
                    sceneGuid = string.Empty;
                }
            }
        }
        #endregion

        public LBSEventHooker()
        {
            registeredActions = new List<UnityActionStored>();
        }

        #region METHODS

        public void OnBeforeSerialize()
        {
            if (Target is not null)
            {
                targetName = _target.name;
                string scenePath = _target.scene.path;
                sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            }
        }

        public void OnAfterDeserialize()
        {
            //throw new NotImplementedException();
        }

        public virtual void Clone(LBSEventHooker eh)
        {
            _target = eh.Target;
            registeredActions = eh.registeredActions;

        }

        public void EventTypeChanged(LBSEventType eventType)
        {
            for (int i = 0; i < RegisteredActions.Count; i++)
            {
                UnityActionStored unityAction = RegisteredActions[i];
                unityAction.eventType = eventType;
                RegisteredActions[i] = unityAction;
            }
        }

        public object Clone()
        {
            LBSEventHooker clone = new LBSEventHooker();
            clone.Clone(this);
            return clone;
        }


        #endregion
    }
}
