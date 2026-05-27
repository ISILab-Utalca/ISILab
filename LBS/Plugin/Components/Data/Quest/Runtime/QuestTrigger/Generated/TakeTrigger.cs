using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class TakeTrigger : QuestTriggerNode
    {
        bool objectTaken;
        [Header("Grammar Fields")]
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("Object to take")]
        private GrammarBundleGraph _Objecttotake;

        protected override void BindFields(List<GrammarField> fields) 
        {
            // Ensure the target field is instantiated so it isn't null
            if (_Objecttotake == null) _Objecttotake = new GrammarBundleGraph();

            var sourceObjecttotake = fields.Find(f => f.name == "Object to take") as GrammarBundleGraph;
            if (sourceObjecttotake != null)
            {
                _Objecttotake.SetValue(sourceObjecttotake.value);
            }
        }

        protected override bool CanComplete() => objectTaken;

        private void OnTriggerStay(Collider other)
        {
            if (IsPlayer(other))
            {
                if(GetInventory(other).HasType(_Objecttotake.value.GUID))
                {
                    objectTaken = true;
                    TryComplete();
                }
            }    
        }
    }
}