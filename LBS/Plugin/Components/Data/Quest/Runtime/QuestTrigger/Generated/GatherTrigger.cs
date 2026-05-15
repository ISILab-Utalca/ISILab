using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class GatherTrigger : QuestTrigger 
    {
        [Header("Grammar Fields")]
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Item Type")]
        private GrammarBundleType _ItemType;

        [SerializeField, InspectorName("Required amount")]
        private GrammarInt _Requiredamount;


        protected override void BindFields(List<GrammarField> fields) 
        {
            var sourceItemType = fields.Find(f => f.name == "Item Type") as GrammarBundleType;
            if (sourceItemType != null) _ItemType.SetValue(sourceItemType.value);

            var sourceRequiredamount = fields.Find(f => f.name == "Required amount") as GrammarInt;
            if (sourceRequiredamount != null) _Requiredamount.SetValue(sourceRequiredamount.value);
        }

        protected override bool CanComplete() => true;
    }
}