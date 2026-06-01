using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class CaptureTrigger : QuestTriggerNode
    {
        [Header("Grammar Fields")]
        [SerializeField, InspectorName("Time to capture")]
        private GrammarFloat _Timetocapture;

        [SerializeField, InspectorName("Reset on exit during capture")]
        private GrammarBool _Resetonexitduringcapture;

        protected override void BindFields(List<GrammarField> fields) 
        {
            _Timetocapture ??= new GrammarFloat();

            var sourceTimetocapture = fields.Find(f => f.name == "Time to capture") as GrammarFloat;
            if (sourceTimetocapture != null)
            {
                _Timetocapture.SetValue(sourceTimetocapture.value);
            }
            _Resetonexitduringcapture ??= new GrammarBool();

            var sourceResetonexitduringcapture = fields.Find(f => f.name == "Reset on exit during capture") as GrammarBool;
            if (sourceResetonexitduringcapture != null)
            {
                _Resetonexitduringcapture.SetValue(sourceResetonexitduringcapture.value);
            }

            this.fields.Add(_Timetocapture);
            this.fields.Add(_Resetonexitduringcapture);
        }

        protected override bool CanComplete() => true;
    }
}