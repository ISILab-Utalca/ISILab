using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class ReadTrigger : QuestTriggerNode
    {
        bool read;

        [Header("Grammar Fields")]
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Object to read")]
        private GrammarBundleGraph _Objecttoread;

        protected override void BindFields(List<GrammarField> fields) 
        {
            // Ensure the target field is instantiated so it isn't null
            if (_Objecttoread == null) _Objecttoread = new GrammarBundleGraph();

            var sourceObjecttoread = fields.Find(f => f.name == "Object to read") as GrammarBundleGraph;
            if (sourceObjecttoread != null)
            {
                _Objecttoread.SetValue(sourceObjecttoread.value);
            }
        }

        protected override bool CanComplete() => read;

        protected override void OnTriggerEnter(Collider other)
        {
            // here replace on trigger enter with some sort of interaction/dialogue system binding
            if (IsPlayer(other))
            {
                read = true;
                TryComplete();
            }
        }
    }
}