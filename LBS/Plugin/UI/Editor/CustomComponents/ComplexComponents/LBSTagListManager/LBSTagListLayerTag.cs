using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
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
    [UxmlElement]
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
        private LBSTagListObject owner;

        private VectorImage unlockedButtonImage = AssetMacro.LoadAssetByGuid<VectorImage>("058d3529b732b9f438e6f92ac1dc6f25");
        private VectorImage lockedButtonImage = AssetMacro.LoadAssetByGuid<VectorImage>("36757362139e77b4f89b892af93c4f16");
        #endregion

        #region VEs
        private VisualElement background;
        private LBSCustomButton deleteButton;
        private Label layerTypeLabel;
        #endregion

        #region PROPERTIES
        public LayerType Type => type;
        public Dictionary<string, Color> LayerColor => layerColor;

        public LBSTagListObject Owner => owner;

        [UxmlAttribute]
        public bool isRemovable
        {
            get => removable;
            set
            {
                removable = value;
                if (deleteButton != null)
                {
                    deleteButton.SetEnabled(removable);
                    deleteButton.style.backgroundImage = new StyleBackground(removable ? unlockedButtonImage : lockedButtonImage);
                }
            }
        }

        #endregion

        #region EVENTS
        public Action<string> OnTypeChanged;
        #endregion

        #region CONSTRUCTORS
        public LBSTagListLayerTag(LBSTagListObject owner, string type, bool removable) : base()
        {
            Init();
            this.owner = owner;
            isRemovable = removable;
            SetType(type);
        }

        public LBSTagListLayerTag() : base()
        {
            Init();
        }
        #endregion

        #region METHODS
        public void Init()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSTagListLayerTag");
            visualTree.CloneTree(this);

            background = this.Q<VisualElement>("MainVE");
            deleteButton = this.Q<LBSCustomButton>("DeleteButton");
            layerTypeLabel = this.Q<Label>("LayerType");

            //Set up delete button
            deleteButton.clicked += () =>
            {
                this.owner.OnLayerTagRemoved?.Invoke();
            };

            OnTypeChanged += SetType;
        }

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
