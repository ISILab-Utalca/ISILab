using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.AI.Grammar
{


    /// <summary>
    /// A rule contains definitions, which would be used to expand the rule into terminal actions
    /// or more rules within. For example:
    /// GoTo:
    ///     go to
    ///     go to -> explore
    ///     Get -> go to
    ///     
    /// Get:
    ///     go to -> steal
    ///     go to -> take
    /// </summary>

    [Serializable]
    public abstract class GrammarElement : ScriptableObject
    {
       
        [SerializeField]
        public string id;

        public abstract VectorImage GetIcon();
    }

}