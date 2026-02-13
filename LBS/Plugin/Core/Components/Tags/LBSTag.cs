using ISILab.Commons.Attributes;
using ISILab.DevTools.Macros;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Components
{
    [CreateAssetMenu(fileName = "NewID", menuName = "ISILab/LBS/Tag")]
    [System.Serializable]
    public class LBSTag : ScriptableObject
    {
        #region FIELDS
        [ReadOnly]
        public string label;
        [SerializeField]
        protected VectorImage icon;
        [SerializeField]
        protected Color color;

        #endregion

        #region PROPERTIES
        public string Label
        {
            get => label;
            set
            {
                if (label == value) return;
                label = value;
                OnChangeText?.Invoke(this);
            }
        }

        public VectorImage Icon
        {
            get
            {
                if (icon == null)
                {
                    return AssetMacro.LoadAssetByGuid<VectorImage>("d6f94a68988be8b45894b9f0e677e8d1");
                }
                return icon;
            }
            set
            {
                if (icon == value)
                    return;

                icon = value;
                OnChangeIcon?.Invoke(this);
            }
        }

        public Color Color
        {
            get => color;
            set
            {
                if (color == value)
                    return;

                color = value;
                OnChangeColor?.Invoke(this);
            }
        }
        #endregion

        #region EVENTS
        public delegate void TagEvent(LBSTag tag);
        public TagEvent OnChangeText;
        public TagEvent OnChangeColor;
        public TagEvent OnChangeIcon;
        public TagEvent OnChangeEntityType;
        #endregion

        #region METHODS
        public void Init(string text, Color color, VectorImage icon)
        {
            this.label = text;
            this.color = color;
            this.icon = icon;
        }

        public override bool Equals(object other)
        {
            if (other is not LBSTag obj) return false;
            return Equals(label, obj.Label);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private void OnValidate()
        {
            label = name;
        }
        #endregion
    }
}

