using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class CaptureTrigger : QuestTrigger 
    {
        [Header("Grammar Fields")]
        [SerializeField, InspectorName("Time to capture")]
        private GrammarFloat _Timetocapture;

        [SerializeField, InspectorName("Reset on exit during capture")]
        private GrammarBool _Resetonexitduringcapture;


        protected override void BindFields(List<GrammarField> fields) 
        {
            var sourceTimetocapture = fields.Find(f => f.name == "Time to capture") as GrammarFloat;
            if (sourceTimetocapture != null) _Timetocapture.SetValue(sourceTimetocapture.value);

            var sourceResetonexitduringcapture = fields.Find(f => f.name == "Reset on exit during capture") as GrammarBool;
            if (sourceResetonexitduringcapture != null) _Resetonexitduringcapture.SetValue(sourceResetonexitduringcapture.value);
        }

        protected override bool CanComplete() => true;
    }
}