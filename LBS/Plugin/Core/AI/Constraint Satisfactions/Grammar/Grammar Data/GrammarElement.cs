using ISILab.LBS.Macros;
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

        [SerializeField]
        public string iconGuid = string.Empty;

        [SerializeField]
        public Color color;

        private VectorImage icon;

        public VectorImage Icon
        {
            get
            {
                if (icon == null && !string.IsNullOrEmpty(iconGuid))
                {
                    icon = LBSAssetMacro.LoadAssetByGuid<VectorImage>(iconGuid);
                }
                return icon;
            }
        }

        public virtual void OnEnable()
        {
            icon = LBSAssetMacro.LoadAssetByGuid<VectorImage>(iconGuid);
        }


    }

}