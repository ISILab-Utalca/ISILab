using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using System;
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
        private Bundle.TagType tagType;
        private string tagName;
        private bool removable;

        private object associatedTag;
        #endregion

        #region VISUAL ELEMENTS
        private LBSCustomButton deleteButton;
        private VisualElement icon;
        private VisualElement layerTagContainer;
        private Label tagLabel;
        private Label groupLabel;
        private LBSTagListLayerTag layerTag;

        private LBSTagListGroup owner;
        #endregion

        #region PROPERTIES
        public string Name => tagName;
        public objectType Type
        {
            get => type;
            set
            {
                type = value;
                groupLabel.SetEnabled(type == objectType.Group);
            }
        }
        public Bundle.TagType TagType => tagType;
        public object AssociatedTag => associatedTag;
        public LBSTagListGroup Owner => owner;
        public bool Removable
        {
            get => removable;
            set
            {
                removable = value;
                deleteButton.SetEnabled(removable);
            }
        }
        #endregion

        #region EVENTS
        public Action OnLayerTagRemoved;
        #endregion

        #region CONSTRUCTORS
        public LBSTagListObject(LBSTagListGroup group, object associatedTag, string tagName, bool removable, bool layerTypeRemovable = false)
        {
            //Basic setup
            this.owner = group;
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSTagListLayerTag");
            visualTree.CloneTree(this);

            //Delete button!
            this.removable = removable;
            deleteButton = this.Q<LBSCustomButton>("DeleteButton");
            deleteButton.SetEnabled(removable);
            deleteButton.clicked += () =>
            {
                owner.OnTagRemoved?.Invoke(layerTag);
            };
            
            //Determine if tag group or not
            this.associatedTag = associatedTag;
            if (associatedTag.GetType() == typeof(LBSTagGroup))
            {
                type = objectType.Group;
            } else if (associatedTag.GetType() == typeof(LBSTag))
            {
                type = objectType.Individual;
            }

            //Set image
            icon = this.Q<VisualElement>("Icon");
            VectorImage imageIcon = type == objectType.Group 
                ? AssetMacro.LoadAssetByGuid<VectorImage>("77d90e8f8c8d77c4e9b1a89d13df5779") 
                : AssetMacro.LoadAssetByGuid<VectorImage>("40d548834301ba14f96af3e1715add5f");
            icon.style.backgroundImage = new StyleBackground(imageIcon);
            
            //Name
            this.tagName = tagName;
            tagLabel = this.Q<Label>("TagName");
            tagLabel.text = tagName;

            //Enable or disable the group label
            groupLabel = this.Q<Label>("GroupLabel");
            if(type == objectType.Individual)
            {
                groupLabel.SetEnabled(false);
            }

            //Lastly, the layer tag!
            layerTagContainer = this.Q<VisualElement>("LayerTagContainer");
            if(type == objectType.Group)
            {
                var associatedTagGroup = associatedTag as LBSTagGroup;
                //Only for groups for now
                if (associatedTagGroup!=null)
                {
                    switch(associatedTagGroup.type)
                    {
                        case LBSTagGroup.TagType.Structural:
                            layerTagContainer.Add(new LBSTagListLayerTag(this, "Interior", layerTypeRemovable));
                            break;
                        case LBSTagGroup.TagType.Aesthetic:
                            layerTagContainer.Add(new LBSTagListLayerTag(this, "Exterior", layerTypeRemovable));
                            break;
                        case LBSTagGroup.TagType.Element:
                            layerTagContainer.Add(new LBSTagListLayerTag(this, "Population", layerTypeRemovable));
                            break;

                    }
                }
                
            }
        }
        #endregion

        #region METHODS
        
        #endregion
    }

}
