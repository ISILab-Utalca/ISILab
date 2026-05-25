using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.AI.Grammar;
using ISILab.LBS.Plugin.MapTools.Generators;
namespace ISILab.AI.Grammar
{
    public class GiveTrigger : QuestTrigger 
    {
        [Commons.Attributes.ReadOnly]
        [SerializeField] private GrammarTerminal _terminal;

        [Header("Grammar Fields")]
    [SerializeField,Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Object to give")] private GrammarBundleGraph _Objecttogive;
    [SerializeField,Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Object to receive")] private GrammarBundleType _Objecttoreceive;

        protected override void SetData(QuestNodeData data) 
        {
            _terminal = data.Terminal;
            _Objecttogive = data.Fields.Find(f => f.name == "Object to give") as GrammarBundleGraph;
        _Objecttoreceive = data.Fields.Find(f => f.name == "Object to receive") as GrammarBundleType;

        }

        protected override bool CanComplete() => false;
    }
}