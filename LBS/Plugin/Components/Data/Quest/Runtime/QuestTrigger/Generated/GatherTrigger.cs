using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.AI.Grammar;
using ISILab.LBS.Plugin.MapTools.Generators;
namespace ISILab.AI.Grammar
{
    public class GatherTrigger : QuestTrigger 
    {
        [Commons.Attributes.ReadOnly]
        [SerializeField] private GrammarTerminal _terminal;

        [Header("Grammar Fields")]
    [SerializeField,Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Item Type")] private GrammarBundleType _ItemType;
    [SerializeField, InspectorName("Required amount")] private GrammarInt _Requiredamount;

        protected override void SetData(QuestNodeData data) 
        {
            _terminal = data.Terminal;
            _ItemType = data.Fields.Find(f => f.name == "Item Type") as GrammarBundleType;
        _Requiredamount = data.Fields.Find(f => f.name == "Required amount") as GrammarInt;

        }

        protected override bool CanComplete() => false;
    }
}