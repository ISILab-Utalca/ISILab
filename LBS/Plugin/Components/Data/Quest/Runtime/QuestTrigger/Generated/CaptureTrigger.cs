using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.AI.Grammar;
using ISILab.LBS.Plugin.MapTools.Generators;
namespace ISILab.AI.Grammar
{
    public class CaptureTrigger : QuestTrigger 
    {
        [Commons.Attributes.ReadOnly]
        [SerializeField] private GrammarTerminal _terminal;

        [Header("Grammar Fields")]
    [SerializeField, InspectorName("Time to capture")] private GrammarFloat _Timetocapture;
    [SerializeField, InspectorName("Reset on exit during capture")] private GrammarBool _Resetonexitduringcapture;

        protected override void SetData(QuestNodeData data) 
        {
            _terminal = data.Terminal;
            _Timetocapture = data.Fields.Find(f => f.name == "Time to capture") as GrammarFloat;
        _Resetonexitduringcapture = data.Fields.Find(f => f.name == "Reset on exit during capture") as GrammarBool;

        }

        protected override bool CanComplete() => false;
    }
}