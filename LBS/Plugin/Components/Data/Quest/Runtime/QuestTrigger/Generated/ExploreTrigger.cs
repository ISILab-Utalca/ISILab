using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.AI.Grammar;
using ISILab.LBS.Plugin.MapTools.Generators;
namespace ISILab.AI.Grammar
{
    public class ExploreTrigger : QuestTrigger 
    {
        [Commons.Attributes.ReadOnly]
        [SerializeField] private GrammarTerminal _terminal;

        [Header("Grammar Fields")]
    [SerializeField, InspectorName("Subareas to enter")] private GrammarAreaList _Subareastoenter;

        protected override void SetData(QuestNodeData data) 
        {
            _terminal = data.Terminal;
            _Subareastoenter = data.Fields.Find(f => f.name == "Subareas to enter") as GrammarAreaList;

        }

        protected override bool CanComplete() => false;
    }
}