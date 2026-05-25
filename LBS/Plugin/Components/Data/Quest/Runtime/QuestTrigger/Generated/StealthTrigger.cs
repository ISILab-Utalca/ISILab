using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.AI.Grammar;
using ISILab.LBS.Plugin.MapTools.Generators;
namespace ISILab.AI.Grammar
{
    public class StealthTrigger : QuestTrigger 
    {
        [Commons.Attributes.ReadOnly]
        [SerializeField] private GrammarTerminal _terminal;

        [Header("Grammar Fields")]
    [SerializeField, InspectorName("Area to reach")] private GrammarArea _Areatoreach;
    [SerializeField, InspectorName("Detectable area")] private GrammarArea _Detectablearea;
    [SerializeField, InspectorName("Area color")] private GrammarColor _Areacolor;

        protected override void SetData(QuestNodeData data) 
        {
            _terminal = data.Terminal;
            _Areatoreach = data.Fields.Find(f => f.name == "Area to reach") as GrammarArea;
        _Detectablearea = data.Fields.Find(f => f.name == "Detectable area") as GrammarArea;
        _Areacolor = data.Fields.Find(f => f.name == "Area color") as GrammarColor;

        }

        protected override bool CanComplete() => false;
    }
}