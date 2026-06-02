using System;
using LBS.Components;
using Newtonsoft.Json;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Editor.Templates
{
    [Serializable]
    [CreateAssetMenu(menuName = "ISILab/LBS/Layer Template")]
    public class LayerTemplate : ScriptableObject
    {
        [JsonRequired, SerializeField]
        private string _templateName;

        [JsonRequired, SerializeField]
        public int order;

        [JsonRequired, SerializeField]
        public LBSLayer layer;

        [JsonIgnore]
        public string Name => _templateName;


        // Editor stuff
        [HideInInspector, NonSerialized]
        public bool _debugView;

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
                // invoke
            }

            foreach (var assistant in layer.Assistants)
            {
                // invoke
                assistant?.OnGUI();
            }
        }
    }
}