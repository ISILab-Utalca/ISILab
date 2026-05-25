using UnityEngine;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class GoToTrigger : QuestTrigger 
    {
        [Commons.Attributes.ReadOnly]
        [SerializeField] private GrammarTerminal _terminal;

    
        protected override void SetData(QuestNodeData data) 
        {
            _terminal = data.Terminal;
    
        }

        protected override bool CanComplete() => false;
    }
}