using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.AI.Grammar;
using ISILab.LBS.Plugin.MapTools.Generators;
namespace ISILab.AI.Grammar
{
    public class ReportTrigger : QuestTrigger 
    {
        [Commons.Attributes.ReadOnly]
        [SerializeField] 
        private GrammarTerminal _terminal;

        [Header("Grammar Fields")]
    [SerializeField,Commons.Attributes.ReadOnlyIncludeChildren, InspectorName("POI to report to")] 
        private GrammarBundleGraph _POItoreportto;

        protected override void SetData(QuestNodeData data) 
        {
            _terminal = data.Terminal;
            _POItoreportto = data.Fields.Find(f => f.name == "POI to report to") as GrammarBundleGraph;

        }

        protected override bool CanComplete() => false;
    }
}