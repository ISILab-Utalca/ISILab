using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class ListenTrigger : QuestTriggerNode
    {
        [Header("Grammar Fields")]
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Object to listen")]
        private GrammarBundleGraph _Objecttolisten;

        protected override void BindFields(List<GrammarField> fields) 
        {
            _Objecttolisten ??= new GrammarBundleGraph();

            var sourceObjecttolisten = fields.Find(f => f.name == "Object to listen") as GrammarBundleGraph;
            if (sourceObjecttolisten != null)
            {
                _Objecttolisten.SetValue(sourceObjecttolisten.value);
            }

            this.fields.Add(_Objecttolisten);
        }

        protected override bool CanComplete() => true;
    }
}