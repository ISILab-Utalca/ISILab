using ISILab.LBS.Plugin.Components.Bundles;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.Editor.UI.CustomComponents
{
    public partial class LBSTagListObject : VisualElement
    {
        #region FIELDS
        public enum objectType { Individual, Group };
        private objectType type;
        private Bundle.EElementFlag flag;
        private string tagName;
        private bool removable;
        #endregion

        #region PROPERTIES
        public string Name => tagName;
        public objectType Type => type;
        public Bundle.EElementFlag Flag => flag;
        public bool Removable => removable;
        #endregion
    }

}
