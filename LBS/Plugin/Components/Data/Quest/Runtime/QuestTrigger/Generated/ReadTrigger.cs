using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.AI.Grammar;
using ISILab.LBS.Plugin.MapTools.Generators;
namespace ISILab.AI.Grammar
{
    public class ReadTrigger : QuestTrigger 
    {
        [Commons.Attributes.ReadOnly]
        [SerializeField] private GrammarTerminal _terminal;

        [Header("Grammar Fields")]
    [SerializeField,Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Object to read")] private GrammarBundleGraph _Objecttoread;

        protected override void SetData(QuestNodeData data) 
        {
            _terminal = data.Terminal;
            _Objecttoread = data.Fields.Find(f => f.name == "Object to read") as GrammarBundleGraph;

        }

        protected override bool CanComplete() => false;
    }
}