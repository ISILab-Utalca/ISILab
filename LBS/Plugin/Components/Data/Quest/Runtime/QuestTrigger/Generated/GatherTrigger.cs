using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class GatherTrigger : QuestTriggerNode
    {
        [Header("Grammar Fields")]
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Item Type")]
        private GrammarBundleType _ItemType;

        [SerializeField, InspectorName("Required amount")]
        private GrammarInt _Requiredamount;

        protected override void BindFields(List<GrammarField> fields) 
        {
            // Ensure the target field is instantiated so it isn't null
            if (_ItemType == null) _ItemType = new GrammarBundleType();

            var sourceItemType = fields.Find(f => f.name == "Item Type") as GrammarBundleType;
            if (sourceItemType != null)
            {
                _ItemType.SetValue(sourceItemType.value);
            }
            // Ensure the target field is instantiated so it isn't null
            if (_Requiredamount == null) _Requiredamount = new GrammarInt();

            var sourceRequiredamount = fields.Find(f => f.name == "Required amount") as GrammarInt;
            if (sourceRequiredamount != null)
            {
                _Requiredamount.SetValue(sourceRequiredamount.value);
            }
        }

        protected override bool CanComplete() => true;
    }
}