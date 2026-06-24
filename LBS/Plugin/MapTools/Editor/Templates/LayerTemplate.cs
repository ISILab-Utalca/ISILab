using System;
using LBS.Components;
using Newtonsoft.Json;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Editor.Templates
{
#if UNITY_EDITOR
    [Serializable]
    [CreateAssetMenu(menuName = "ISILab/LBS/Layer Template")]
    public class LayerTemplate : ScriptableObject
    {
        [JsonRequired, SerializeField]
        private string _templateName;

        [JsonRequired, SerializeField]
        public int Order;

        [JsonRequired, SerializeField]
        public LBSLayer layer;

        [JsonIgnore]
        public string Name => _templateName;


        // Editor stuff
        [HideInInspector, NonSerialized]
        public bool DebugView;

        public void Clear()
        {
            layer = new LBSLayer();
        }

        private void OnValidate()
        {
            if (layer == null) return;
            foreach (var behaviour in layer.Behaviours)
            {
                behaviour?.OnGUI();
            }

            foreach (var assistant in layer.Assistants)
            {
                assistant?.OnGUI();
            }
        }

        public void SetName(string newName)
        {
            if (!string.IsNullOrEmpty(newName))
            {
                _templateName = newName;
            }
        }
    }
#endif
}