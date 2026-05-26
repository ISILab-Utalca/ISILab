using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class SpyTrigger : QuestTriggerNode
    {
        [Header("Grammar Fields")]
        [SerializeField, InspectorName("Min distance")]
        private GrammarInt _Mindistance;

        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("POI to spy")]
        private GrammarBundleGraph _POItospy;

        protected override void BindFields(List<GrammarField> fields) 
        {
            // Ensure the target field is instantiated so it isn't null
            if (_Mindistance == null) _Mindistance = new GrammarInt();

            var sourceMindistance = fields.Find(f => f.name == "Min distance") as GrammarInt;
            if (sourceMindistance != null)
            {
                _Mindistance.SetValue(sourceMindistance.value);
            }
            // Ensure the target field is instantiated so it isn't null
            if (_POItospy == null) _POItospy = new GrammarBundleGraph();

            var sourcePOItospy = fields.Find(f => f.name == "POI to spy") as GrammarBundleGraph;
            if (sourcePOItospy != null)
            {
                _POItospy.SetValue(sourcePOItospy.value);
            }
        }

        protected override bool CanComplete() => true;
    }
}