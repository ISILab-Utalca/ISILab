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
    [UxmlElement]
    public partial class LBSTagListObject : LBSCustomListItem
    {
        #region FIELDS
        public enum objectType { Individual, Group };
        private objectType type;
        private Bundle.TagType tagType;
        private string tagName = "Tag";
        private bool removable = true;

        private object associatedTag = null;
        #endregion

        #region VISUAL ELEMENTS
        private LBSToolbarButton deleteButton;
        private VisualElement icon;
        private VisualElement layerTagContainer;
        private Label tagLabel;
        private Label groupLabel;
        private LBSTagListLayerTag layerTag;

        private LBSTagListGroup owner;
        #endregion

        #region PROPERTIES
        public string Name {
            get => tagName;
            set
            {
                tagName = value;
                tagLabel.text = tagName;
            }
        }
        public objectType Type
        {
            get => type;
            set
            {
                type = value;
                groupLabel.SetEnabled(type == objectType.Group);
            }
        }
        public Bundle.TagType TagType
        {
            get => tagType;
            set => tagType = value;
        }
        public object AssociatedTag
        {
            get => associatedTag;
            set
            {
                associatedTag = value;
                if(associatedTag.GetType() ==typeof(LBSTagGroup)) {
                    AddLayerTag();
                }
            }
        }
        public LBSTagListGroup Owner
        {
            get => owner;
            set => owner = value;
        }

        [UxmlAttribute]
        public bool IsRemovable
        {
            get => removable;
            set
            {
                removable = value;
                deleteButton.SetEnabled(removable);
                deleteButton.style.visibility = IsRemovable ? Visibility.Visible : Visibility.Hidden;
                deleteButton.style.display = IsRemovable ? DisplayStyle.Flex : DisplayStyle.None;

            }
        }
        #endregion

        #region EVENTS
        public Action OnOwnerChanged;
        public Action OnLayerTagRemoved;
        #endregion

        #region CONSTRUCTORS
        public LBSTagListObject()
        {
            Init();
        }

        public LBSTagListObject(LBSTagListGroup group, object associatedTag, bool removable, bool layerTypeRemovable = false) : base()
        {
            Init();

            //Basic setup
            this.owner = group;
            
            //Delete button!
            this.removable = removable;
            deleteButton.SetEnabled(removable);
            
            //Determine if tag group or not
            this.associatedTag = associatedTag;
            if (associatedTag.GetType() == typeof(LBSTagGroup))
            {
                var groupTag = associatedTag as LBSTagGroup;
                tagName = groupTag.name;
                type = objectType.Group;
            } else if (associatedTag.GetType() == typeof(LBSTag))
            {
                var indivTag = associatedTag as LBSTag;
                tagName = indivTag.label;
                type = objectType.Individual;
            }
            Debug.Log("loading " + tagName + " as " + type);

            //Set image
            icon = this.Q<VisualElement>("Icon");
            VectorImage imageIcon = type == objectType.Group 
                ? AssetMacro.LoadAssetByGuid<VectorImage>("77d90e8f8c8d77c4e9b1a89d13df5779") 
                : AssetMacro.LoadAssetByGuid<VectorImage>("40d548834301ba14f96af3e1715add5f");
            icon.style.backgroundImage = new StyleBackground(imageIcon);
            
            //Name
            tagLabel.text = tagName;

            //Enable or disable the group label
            if(type == objectType.Individual)
            {
                groupLabel.SetEnabled(false);
            }

            //Lastly, the layer tag!
            if(type == objectType.Group)
            {
                var associatedTagGroup = associatedTag as LBSTagGroup;
                layerTag = new LBSTagListLayerTag(this, "Interior", layerTypeRemovable);
                //Only for groups for now
                if (associatedTagGroup!=null)
                {
                    switch(associatedTagGroup.type)
                    {
                        case LBSTagGroup.TagType.Structural:
                            layerTag = new LBSTagListLayerTag(this, "Interior", layerTypeRemovable);
                            break;
                        case LBSTagGroup.TagType.Aesthetic:
                            layerTag = new LBSTagListLayerTag(this, "Exterior", layerTypeRemovable);
                            break;
                        case LBSTagGroup.TagType.Element:
                            layerTag = new LBSTagListLayerTag(this, "Population", layerTypeRemovable);
                            break;

                    }
                    layerTagContainer.Add(layerTag);
                }
                
            }
        }
        #endregion

        #region METHODS
        public void Init()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSTagListObject");
            visualTree.CloneTree(this);

            deleteButton = this.Q<LBSToolbarButton>("DeleteButton");
            deleteButton.clicked += () =>
            {
                if (owner != null)
                {
                    owner.OnTagRemoved?.Invoke(layerTag);
                }
            };
            tagLabel = this.Q<Label>("TagName");
            groupLabel = this.Q<Label>("GroupLabel");
            layerTagContainer = this.Q<VisualElement>("LayerTagContainer");
        }

        public void AddLayerTag()
        {
            layerTagContainer.Clear();
            //Lastly, the layer tag!
            if (type == objectType.Group)
            {
                var associatedTagGroup = associatedTag as LBSTagGroup;
                //Only for groups for now
                if (associatedTagGroup != null)
                {
                    switch (associatedTagGroup.type)
                    {
                        case LBSTagGroup.TagType.Structural:
                            layerTagContainer.Add(new LBSTagListLayerTag(this, "Interior", false));
                            break;
                        case LBSTagGroup.TagType.Aesthetic:
                            layerTagContainer.Add(new LBSTagListLayerTag(this, "Exterior", false));
                            break;
                        case LBSTagGroup.TagType.Element:
                            layerTagContainer.Add(new LBSTagListLayerTag(this, "Population", false));
                            break;

                    }
                }

            }
        }
        #endregion
    }

}
