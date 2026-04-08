using CodiceApp;
using ISILab.LBS.Plugin.Components.Bundles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.Editor.UI.CustomComponents
{
    public partial class LBSTagListLayerTag : VisualElement
    {
        #region FIELDS
        public enum LayerType { Exterior, Interior, Population, Quest };
        protected static Dictionary<string, Color> layerColor = new Dictionary<string, Color>
        {
            ["Exterior"] = new Color32(51, 76, 26, 255), //green
            ["Interior"] = new Color32(76, 26, 26, 255), //red
            ["Population"] = new Color32(63, 13, 75, 255), // purple
            ["Quest"] = new Color32(12, 54, 75, 255) //blue
        };
        private LayerType type;
        private bool removable;

        #endregion

        #region PROPERTIES
        public LayerType Type => type;
        public Dictionary<string, Color> LayerColor => layerColor;
        public bool Removable => removable;
        #endregion

        #region EVENTS
        public Event OnTypeChanged;
        public Event OnRemovableChanged;
        #endregion

        #region CONSTRUCTORS
        public LBSTagListLayerTag(string type, bool removable)
        {

        }
        #endregion
    }

}
