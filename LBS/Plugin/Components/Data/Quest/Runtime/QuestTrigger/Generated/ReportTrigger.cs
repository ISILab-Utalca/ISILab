using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class ReportTrigger : QuestTriggerNode
    {
        [Header("Grammar Fields")]
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("POI to report to")]
        private GrammarBundleGraph _POItoreportto;

        protected override void BindFields(List<GrammarField> fields) 
        {
            _POItoreportto ??= new GrammarBundleGraph();

            var sourcePOItoreportto = fields.Find(f => f.name == "POI to report to") as GrammarBundleGraph;
            if (sourcePOItoreportto != null)
            {
                _POItoreportto.SetValue(sourcePOItoreportto.value);
            }

            this.fields.Add(_POItoreportto);
        }

        protected override bool CanComplete() => true;
    }
}