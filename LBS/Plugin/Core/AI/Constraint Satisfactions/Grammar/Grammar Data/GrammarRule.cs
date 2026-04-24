using ISILab.LBS.Macros;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.AI.Grammar
{

    [Serializable]
    public class GrammarExpansion
    {
        public List<string> sequence = new();
    }

    [Serializable]
    public class GrammarRule : GrammarElement
    {
        private const string defaultIconGuid = "d7b2c9af591bee4429f705ae7ae6abea";
        private static readonly Color defaultColor = Color.white;
        // a suequence of strings(id's of the terminal actions or rules)
        [SerializeField]
        public List<GrammarExpansion> Expansions = new();

        public override void OnEnable()
        {
            color = defaultColor;
            iconGuid = defaultIconGuid;
            base.OnEnable();
        }
    }

}