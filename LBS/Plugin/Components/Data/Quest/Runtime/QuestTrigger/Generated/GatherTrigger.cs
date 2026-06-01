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
            _ItemType ??= new GrammarBundleType();

            var sourceItemType = fields.Find(f => f.name == "Item Type") as GrammarBundleType;
            if (sourceItemType != null)
            {
                _ItemType.SetValue(sourceItemType.value);
            }
            _Requiredamount ??= new GrammarInt();

            var sourceRequiredamount = fields.Find(f => f.name == "Required amount") as GrammarInt;
            if (sourceRequiredamount != null)
            {
                _Requiredamount.SetValue(sourceRequiredamount.value);
            }

            this.fields.Add(_ItemType);
            this.fields.Add(_Requiredamount);
        }

        protected override bool CanComplete() => true;
    }
}