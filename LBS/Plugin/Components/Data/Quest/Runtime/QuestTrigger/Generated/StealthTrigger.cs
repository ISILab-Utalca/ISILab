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
            _Areatoreach ??= new GrammarArea();

            var sourceAreatoreach = fields.Find(f => f.name == "Area to reach") as GrammarArea;
            if (sourceAreatoreach != null)
            {
                _Areatoreach.SetValue(sourceAreatoreach.value);
            }
            _Detectablearea ??= new GrammarArea();

            var sourceDetectablearea = fields.Find(f => f.name == "Detectable area") as GrammarArea;
            if (sourceDetectablearea != null)
            {
                _Detectablearea.SetValue(sourceDetectablearea.value);
            }
            _Areacolor ??= new GrammarColor();

            var sourceAreacolor = fields.Find(f => f.name == "Area color") as GrammarColor;
            if (sourceAreacolor != null)
            {
                _Areacolor.SetValue(sourceAreacolor.value);
            }

            this.fields.Add(_Areatoreach);
            this.fields.Add(_Detectablearea);
            this.fields.Add(_Areacolor);
        }

        protected override bool CanComplete() => true;
    }
}