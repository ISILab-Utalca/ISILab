using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class SpyTrigger : QuestTrigger 
    {
        [Header("Grammar Fields")]
        [SerializeField, InspectorName("Min distance")]
        private GrammarInt _Mindistance;

        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("POI to spy")]
        private GrammarBundleGraph _POItospy;


        protected override void BindFields(List<GrammarField> fields) 
        {
            var sourceMindistance = fields.Find(f => f.name == "Min distance") as GrammarInt;
            if (sourceMindistance != null) _Mindistance.SetValue(sourceMindistance.value);

            var sourcePOItospy = fields.Find(f => f.name == "POI to spy") as GrammarBundleGraph;
            if (sourcePOItospy != null) _POItospy.SetValue(sourcePOItospy.value);
        }

        protected override bool CanComplete() => true;
    }
}