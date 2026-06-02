using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class ExchangeTrigger : QuestTriggerNode
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
            _Typetogive ??= new GrammarBundleType();

            var sourceTypetogive = fields.Find(f => f.name == "Type to give") as GrammarBundleType;
            if (sourceTypetogive != null)
            {
                _Typetogive.SetValue(sourceTypetogive.value);
            }
            _Amounttogive ??= new GrammarInt();

            var sourceAmounttogive = fields.Find(f => f.name == "Amount to give") as GrammarInt;
            if (sourceAmounttogive != null)
            {
                _Amounttogive.SetValue(sourceAmounttogive.value);
            }
            _Typetoreceive ??= new GrammarBundleType();

            var sourceTypetoreceive = fields.Find(f => f.name == "Type to receive") as GrammarBundleType;
            if (sourceTypetoreceive != null)
            {
                _Typetoreceive.SetValue(sourceTypetoreceive.value);
            }
            _Amounttoreceive ??= new GrammarInt();

            var sourceAmounttoreceive = fields.Find(f => f.name == "Amount to receive") as GrammarInt;
            if (sourceAmounttoreceive != null)
            {
                _Amounttoreceive.SetValue(sourceAmounttoreceive.value);
            }

            this.fields.Add(_Typetogive);
            this.fields.Add(_Amounttogive);
            this.fields.Add(_Typetoreceive);
            this.fields.Add(_Amounttoreceive);
        }

        protected override bool CanComplete() => true;
    }
}