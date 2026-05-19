using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class StealthTrigger : QuestTriggerNode
    {
        [Header("Grammar Fields")]
        [SerializeField, InspectorName("Area to reach")]
        private GrammarArea _Areatoreach;

        [SerializeField, InspectorName("Detectable area")]
        private GrammarArea _Detectablearea;

        [SerializeField, InspectorName("Area color")]
        private GrammarColor _Areacolor;

        protected override void BindFields(List<GrammarField> fields) 
        {
            // Ensure the target field is instantiated so it isn't null
            if (_Areatoreach == null) _Areatoreach = new GrammarArea();

            var sourceAreatoreach = fields.Find(f => f.name == "Area to reach") as GrammarArea;
            if (sourceAreatoreach != null)
            {
                _Areatoreach.SetValue(sourceAreatoreach.value);
            }
            // Ensure the target field is instantiated so it isn't null
            if (_Detectablearea == null) _Detectablearea = new GrammarArea();

            var sourceDetectablearea = fields.Find(f => f.name == "Detectable area") as GrammarArea;
            if (sourceDetectablearea != null)
            {
                _Detectablearea.SetValue(sourceDetectablearea.value);
            }
            // Ensure the target field is instantiated so it isn't null
            if (_Areacolor == null) _Areacolor = new GrammarColor();

            var sourceAreacolor = fields.Find(f => f.name == "Area color") as GrammarColor;
            if (sourceAreacolor != null)
            {
                _Areacolor.SetValue(sourceAreacolor.value);
            }
        }

        protected override bool CanComplete() => true;
    }
}