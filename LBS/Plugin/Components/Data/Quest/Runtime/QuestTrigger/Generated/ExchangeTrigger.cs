using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.AI.Grammar;
using ISILab.LBS.Plugin.MapTools.Generators;
namespace ISILab.AI.Grammar
{
    public class ExchangeTrigger : QuestTrigger 
    {
        [Commons.Attributes.ReadOnly]
        [SerializeField] private GrammarTerminal _terminal;

        [Header("Grammar Fields")]
    [SerializeField,Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Type to give")] private GrammarBundleType _Typetogive;
    [SerializeField, InspectorName("Amount to give")] private GrammarInt _Amounttogive;
    [SerializeField,Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Type to receive")] private GrammarBundleType _Typetoreceive;
    [SerializeField, InspectorName("Amount to receive")] private GrammarInt _Amounttoreceive;

        protected override void SetData(QuestNodeData data) 
        {
            _terminal = data.Terminal;
            _Typetogive = data.Fields.Find(f => f.name == "Type to give") as GrammarBundleType;
        _Amounttogive = data.Fields.Find(f => f.name == "Amount to give") as GrammarInt;
        _Typetoreceive = data.Fields.Find(f => f.name == "Type to receive") as GrammarBundleType;
        _Amounttoreceive = data.Fields.Find(f => f.name == "Amount to receive") as GrammarInt;

        }

        protected override bool CanComplete() => false;
    }
}