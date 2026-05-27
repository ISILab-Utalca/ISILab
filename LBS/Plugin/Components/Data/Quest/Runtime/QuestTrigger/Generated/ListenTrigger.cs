using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class ListenTrigger : QuestTriggerNode
    {
        bool listened;

        [Header("Grammar Fields")]
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Object to listen")]
        private GrammarBundleGraph _Objecttolisten;

        protected override void BindFields(List<GrammarField> fields) 
        {
            // Ensure the target field is instantiated so it isn't null
            if (_Objecttolisten == null) _Objecttolisten = new GrammarBundleGraph();

            var sourceObjecttolisten = fields.Find(f => f.name == "Object to listen") as GrammarBundleGraph;
            if (sourceObjecttolisten != null)
            {
                _Objecttolisten.SetValue(sourceObjecttolisten.value);
            }
        }

        protected override bool CanComplete() => listened;

        protected override void OnTriggerEnter(Collider other)
        {
            // here replace on trigger enter with some sort of interaction/dialogue system binding
            if (IsPlayer(other))
            {
                listened = true;
                TryComplete();
            }
        }
    }
}