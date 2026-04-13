using ISILab.LBS.Macros;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
        private const string defaultTerminalIcon = "bb0770b945366c94c822cf3255eb885d";

        [SerializeField]
        private string iconGuid = defaultTerminalIcon;

        [SerializeField] 
        public List<GrammarField> fields = new();

        public override VectorImage GetIcon()
    => LBSAssetMacro.LoadAssetByGuid<VectorImage>(iconGuid);
    }
}