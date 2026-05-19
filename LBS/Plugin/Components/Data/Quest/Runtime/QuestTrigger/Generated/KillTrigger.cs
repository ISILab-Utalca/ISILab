using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class KillTrigger : QuestTriggerNode
    {
        [Header("Grammar Fields")]
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Type to kill")]
        private GrammarBundleType _Typetokill;

        [SerializeField, InspectorName("Required kills")]
        private GrammarInt _Requiredkills;

        protected override void BindFields(List<GrammarField> fields) 
        {
            // Ensure the target field is instantiated so it isn't null
            if (_Typetokill == null) _Typetokill = new GrammarBundleType();

            var sourceTypetokill = fields.Find(f => f.name == "Type to kill") as GrammarBundleType;
            if (sourceTypetokill != null)
            {
                _Typetokill.SetValue(sourceTypetokill.value);
            }
            // Ensure the target field is instantiated so it isn't null
            if (_Requiredkills == null) _Requiredkills = new GrammarInt();

            var sourceRequiredkills = fields.Find(f => f.name == "Required kills") as GrammarInt;
            if (sourceRequiredkills != null)
            {
                _Requiredkills.SetValue(sourceRequiredkills.value);
            }
        }

        protected override bool CanComplete() => true;
    }
}