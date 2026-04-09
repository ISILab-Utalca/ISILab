using CodiceApp;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.UI.Editor.Windows.TagManager;
using System;
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

        #region VEs
        private VisualElement background;
        private LBSCustomButton deleteButton;
        private LBSCustomButton lockedButton;
        private Label layerTypeLabel;

        private LBSTagListObject owner;
        #endregion

        #region PROPERTIES
        public LayerType Type => type;
        public Dictionary<string, Color> LayerColor => layerColor;
        public bool Removable
        {
            get => removable;
            set
            {
                removable = value;
                if (deleteButton != null)
                {
                    deleteButton.SetEnabled(removable);
                    deleteButton.style.visibility = removable ? Visibility.Visible : Visibility.Hidden;
                    lockedButton.style.visibility = removable ? Visibility.Hidden : Visibility.Visible;
                    deleteButton.style.display = removable ? DisplayStyle.Flex : DisplayStyle.None;
                    lockedButton.style.display = removable ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }
        }
    
        #endregion

        #region EVENTS
        public Action<string> OnTypeChanged;
        #endregion

        #region CONSTRUCTORS
        public LBSTagListLayerTag(LBSTagListObject owner, string type, bool removable)
        {
            this.owner = owner;
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSTagListLayerTag");
            visualTree.CloneTree(this);

            background = this.Q<VisualElement>("MainVE");
            deleteButton = this.Q<LBSCustomButton>("DeleteButton");
            lockedButton = this.Q<LBSCustomButton>("LockedButton");
            layerTypeLabel = this.Q<Label>("LayerType");

            //Set up delete button
            deleteButton.clicked += () =>
            {
                this.owner.OnLayerTagRemoved?.Invoke();
            };

            //Set its type
            SetType(type);
            OnTypeChanged += SetType;

            //Set up remove button
            Removable = removable;
        }
        #endregion

        #region METHODS
        public void SetType(string newTypeString)
        {
            LayerType newType;
            bool newParse = Enum.TryParse(newTypeString, out newType);
            if (!newParse) return;

            type = newType;
            if(layerColor.ContainsKey(newTypeString))
            {
                background.SetBackgroundColor(layerColor[newTypeString]);
                layerTypeLabel.text = newTypeString;
            }
        }
        #endregion
    }

}
