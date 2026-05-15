using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class ListenTrigger : QuestTrigger 
    {
        [Header("Grammar Fields")]
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Object to listen")]
        private GrammarBundleGraph _Objecttolisten;


        protected override void BindFields(List<GrammarField> fields) 
        {
            var sourceObjecttolisten = fields.Find(f => f.name == "Object to listen") as GrammarBundleGraph;
            if (sourceObjecttolisten != null) _Objecttolisten.SetValue(sourceObjecttolisten.value);
        }

        protected override bool CanComplete() => true;
    }
}