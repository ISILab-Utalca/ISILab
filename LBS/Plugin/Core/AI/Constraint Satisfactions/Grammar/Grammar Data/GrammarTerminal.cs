using ISILab.LBS.Plugin.Core.Settings;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.AI.Grammar
{


    /// <summary>
    /// The action that gest added into the graph for example:
    ///     go to
    ///     explore
    ///     kill
    /// </summary>
    [Serializable]
    public class GrammarTerminal : GrammarElement
    {
        private const string defaultIconGuid = "bb0770b945366c94c822cf3255eb885d";
        private static readonly Color fallbackColor = Color.white;

        // polymorphic classes use ref
        [SerializeReference]
        public List<GrammarField> fields = new();

        public override void OnEnable()
        {
            // Safe: happens after Unity initialization
            var settings = LBSSettings.Instance;

            if(color == null)
            {
                color = settings != null
                ? settings.view.behavioursColor
                : fallbackColor;
            }
      
            iconGuid = defaultIconGuid;

            base.OnEnable();
        }
    }
}