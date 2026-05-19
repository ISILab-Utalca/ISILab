using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{
    public class GoToTrigger : QuestTriggerNode
    {


        protected override void BindFields(List<GrammarField> fields) 
        {

        }

        protected override bool CanComplete() => true;
    }
}