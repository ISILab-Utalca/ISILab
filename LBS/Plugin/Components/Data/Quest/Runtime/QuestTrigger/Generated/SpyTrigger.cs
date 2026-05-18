using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.AI.Grammar;
using ISILab.LBS.Plugin.MapTools.Generators;
namespace ISILab.AI.Grammar
{
    public class SpyTrigger : QuestTrigger 
    {
        [Commons.Attributes.ReadOnly]
        [SerializeField] private GrammarTerminal _terminal;

        [Header("Grammar Fields")]
    [SerializeField, InspectorName("Min distance")] private GrammarInt _Mindistance;
    [SerializeField,Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("POI to spy")] private GrammarBundleGraph _POItospy;

        protected override void SetData(QuestNodeData data) 
        {
            _terminal = data.Terminal;
            _Mindistance = data.Fields.Find(f => f.name == "Min distance") as GrammarInt;
        _POItospy = data.Fields.Find(f => f.name == "POI to spy") as GrammarBundleGraph;

        }

        protected override bool CanComplete() => false;
    }
}