using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.AI.Grammar;
using ISILab.LBS.Plugin.MapTools.Generators;
namespace ISILab.AI.Grammar
{
    public class ListenTrigger : QuestTrigger 
    {
        [Commons.Attributes.ReadOnly]
        [SerializeField] private GrammarTerminal _terminal;

        [Header("Grammar Fields")]
    [SerializeField,Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Object to listen")] private GrammarBundleGraph _Objecttolisten;

        protected override void SetData(QuestNodeData data) 
        {
            _terminal = data.Terminal;
            _Objecttolisten = data.Fields.Find(f => f.name == "Object to listen") as GrammarBundleGraph;

        }

        protected override bool CanComplete() => false;
    }
}