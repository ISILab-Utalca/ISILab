using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class ExchangeTrigger : QuestTrigger 
    {
        [Header("Grammar Fields")]
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Type to give")]
        private GrammarBundleType _Typetogive;

        [SerializeField, InspectorName("Amount to give")]
        private GrammarInt _Amounttogive;

        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Type to receive")]
        private GrammarBundleType _Typetoreceive;

        [SerializeField, InspectorName("Amount to receive")]
        private GrammarInt _Amounttoreceive;


        protected override void BindFields(List<GrammarField> fields) 
        {
            var sourceTypetogive = fields.Find(f => f.name == "Type to give") as GrammarBundleType;
            if (sourceTypetogive != null) _Typetogive.SetValue(sourceTypetogive.value);

            var sourceAmounttogive = fields.Find(f => f.name == "Amount to give") as GrammarInt;
            if (sourceAmounttogive != null) _Amounttogive.SetValue(sourceAmounttogive.value);

            var sourceTypetoreceive = fields.Find(f => f.name == "Type to receive") as GrammarBundleType;
            if (sourceTypetoreceive != null) _Typetoreceive.SetValue(sourceTypetoreceive.value);

            var sourceAmounttoreceive = fields.Find(f => f.name == "Amount to receive") as GrammarInt;
            if (sourceAmounttoreceive != null) _Amounttoreceive.SetValue(sourceAmounttoreceive.value);
        }

        protected override bool CanComplete() => true;
    }
}