using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class TakeTrigger : QuestTrigger 
    {
        [Header("Grammar Fields")]
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Object to take")]
        private GrammarBundleGraph _Objecttotake;


        protected override void BindFields(List<GrammarField> fields) 
        {
            var sourceObjecttotake = fields.Find(f => f.name == "Object to take") as GrammarBundleGraph;
            if (sourceObjecttotake != null) _Objecttotake.SetValue(sourceObjecttotake.value);
        }

        protected override bool CanComplete() => true;
    }
}