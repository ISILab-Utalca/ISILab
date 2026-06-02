using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class GiveTrigger : QuestTriggerNode
    {
        [Header("Grammar Fields")]
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Object to give")]
        private GrammarBundleGraph _Objecttogive;

        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Object to receive")]
        private GrammarBundleType _Objecttoreceive;

        protected override void BindFields(List<GrammarField> fields) 
        {
            _Objecttogive ??= new GrammarBundleGraph();

            var sourceObjecttogive = fields.Find(f => f.name == "Object to give") as GrammarBundleGraph;
            if (sourceObjecttogive != null)
            {
                _Objecttogive.SetValue(sourceObjecttogive.value);
            }
            _Objecttoreceive ??= new GrammarBundleType();

            var sourceObjecttoreceive = fields.Find(f => f.name == "Object to receive") as GrammarBundleType;
            if (sourceObjecttoreceive != null)
            {
                _Objecttoreceive.SetValue(sourceObjecttoreceive.value);
            }

            this.fields.Add(_Objecttogive);
            this.fields.Add(_Objecttoreceive);
        }

        protected override bool CanComplete() => true;
    }
}