using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class ExploreTrigger : QuestTrigger 
    {
        [Header("Grammar Fields")]
        [SerializeField, InspectorName("Subareas to enter")]
        private GrammarAreaList _Subareastoenter;


        protected override void BindFields(List<GrammarField> fields) 
        {
            var sourceSubareastoenter = fields.Find(f => f.name == "Subareas to enter") as GrammarAreaList;
            if (sourceSubareastoenter != null) _Subareastoenter.SetValue(sourceSubareastoenter.value);
        }

        protected override bool CanComplete() => true;
    }
}