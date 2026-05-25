using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.AI.Grammar;
using ISILab.LBS.Plugin.MapTools.Generators;
namespace ISILab.AI.Grammar
{
    public class KillTrigger : QuestTrigger 
    {
        [Commons.Attributes.ReadOnly]
        [SerializeField] private GrammarTerminal _terminal;

        [Header("Grammar Fields")]
    [SerializeField,Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Type to kill")] private GrammarBundleType _Typetokill;
    [SerializeField, InspectorName("Required kills")] private GrammarInt _Requiredkills;

        protected override void SetData(QuestNodeData data) 
        {
            _terminal = data.Terminal;
            _Typetokill = data.Fields.Find(f => f.name == "Type to kill") as GrammarBundleType;
        _Requiredkills = data.Fields.Find(f => f.name == "Required kills") as GrammarInt;

        }

        protected override bool CanComplete() => false;
    }
}