using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class KillTrigger : QuestTrigger 
    {
        [Header("Grammar Fields")]
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Type to kill")]
        private GrammarBundleType _Typetokill;

        [SerializeField, InspectorName("Required kills")]
        private GrammarInt _Requiredkills;


        protected override void BindFields(List<GrammarField> fields) 
        {
            var sourceTypetokill = fields.Find(f => f.name == "Type to kill") as GrammarBundleType;
            if (sourceTypetokill != null) _Typetokill.SetValue(sourceTypetokill.value);

            var sourceRequiredkills = fields.Find(f => f.name == "Required kills") as GrammarInt;
            if (sourceRequiredkills != null) _Requiredkills.SetValue(sourceRequiredkills.value);
        }

        protected override bool CanComplete() => true;
    }
}