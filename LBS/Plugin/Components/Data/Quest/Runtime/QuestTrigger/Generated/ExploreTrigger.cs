using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class ExploreTrigger : QuestTriggerNode
    {
        public bool Visited;

        [Header("Grammar Fields")]
        [SerializeField, InspectorName("Subareas to enter")]
        private GrammarAreaList _Subareastoenter;

        protected override void BindFields(List<GrammarField> fields) 
        {
            // Ensure the target field is instantiated so it isn't null
            if (_Subareastoenter == null) _Subareastoenter = new GrammarAreaList();

            var sourceSubareastoenter = fields.Find(f => f.name == "Subareas to enter") as GrammarAreaList;
            if (sourceSubareastoenter != null)
            {
                _Subareastoenter.SetValue(sourceSubareastoenter.value);
            }
        }

        protected override bool CanComplete()
        {
            var subAreasGOs = GetComponentsInChildren<ExploreTrigger>();
            foreach(var subAreaGo in subAreasGOs)
            {
                if (!subAreaGo.Visited) return false;
            }
            return true;
        }

        protected override void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other))
            {
                Visited = true;
                TryComplete();
            }
        }
    }
}