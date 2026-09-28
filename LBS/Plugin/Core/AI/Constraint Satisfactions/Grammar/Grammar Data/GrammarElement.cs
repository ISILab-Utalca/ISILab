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

        [SerializeField, HideInInspector]
        public string iconGuid = string.Empty;

        [SerializeField]
        public Color color = Color.white;
        [SerializeField]
        private VectorImage icon;

        public VectorImage Icon
        {
            get
            {
#if UNITY_EDITOR
                if (icon == null && !string.IsNullOrEmpty(iconGuid))
                {
                    icon = LBSAssetMacro.LoadAssetByGuid<VectorImage>(iconGuid);
                }
#endif
                return icon;
            }

            set
            {
                icon = value;
#if UNITY_EDITOR
                iconGuid = LBSAssetMacro.GetGuidFromAsset(icon);
#endif
            }
        }

        public virtual void OnEnable()
        {
#if UNITY_EDITOR
            // on load get vector image by guid. VectorImage is not serialized
            Icon = LBSAssetMacro.LoadAssetByGuid<VectorImage>(iconGuid);
#endif
        }

        protected virtual void OnValidate()
        {
            if (icon != null)
            {
                Icon = icon;
            }
        }
    }

}