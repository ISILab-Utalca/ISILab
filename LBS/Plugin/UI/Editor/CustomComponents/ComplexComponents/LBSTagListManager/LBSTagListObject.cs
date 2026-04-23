using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor.Windows.TagManager;
using System;
using System.Collections;
using UnityEditor;
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

        private ScriptableObject associatedTag = null;
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
                groupLabel.style.visibility = type == objectType.Group ? Visibility.Visible : Visibility.Hidden;
                groupLabel.style.display = type == objectType.Group ? DisplayStyle.Flex : DisplayStyle.None;

                var imageIcon = type == objectType.Group
                    ? AssetMacro.LoadAssetByGuid<VectorImage>("77d90e8f8c8d77c4e9b1a89d13df5779")
                    : AssetMacro.LoadAssetByGuid<VectorImage>("40d548834301ba14f96af3e1715add5f");
                icon.style.backgroundImage = new StyleBackground(imageIcon);
            }
        }
        public Bundle.TagType TagType
        {
            get => tagType;
            set => tagType = value;
        }
        public ScriptableObject AssociatedTag
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

        public LBSTagListObject(LBSTagListGroup group, ScriptableObject associatedTag, bool removable, bool layerTypeRemovable = false) : base()
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

            //Set image
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
                if(owner.AssociatedTag!=null)
                {
                    var choice = EditorUtility.DisplayDialogComplex("Removal Options", "Choose the operation you want to proceed with:", "Remove from Group", "Delete Tag", "Cancel");
                    switch(choice)
                    {
                        case 0: RemoveFromGroup(); break;
                        case 1: DeleteTag(); break;
                        case 2: break;
                    }
                } else
                {
                    DeleteTag();
                }
            };
            icon = this.Q<VisualElement>("Icon");
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
        
        public void RemoveFromGroup()
        {
            var answer = EditorUtility.DisplayDialog("Remove Tag?", "Removing this tag from its associated group. Proceed?", "Continue", "Cancel");

            //We know the associated tag isn't null, so we can freely call it
            if(answer)
            {
                //Remove the tag from the grouptag's list, which has to be made from there.
                owner.OnTagRemoved?.Invoke(associatedTag);

                //Then remove the tag from the associated tag group.
                var ownerTag = owner.AssociatedTag as LBSTagGroup;

                var toRemove = associatedTag as LBSTag;
                if (ownerTag != null)
                {
                    if (ownerTag.Tags.Contains(toRemove))
                    {
                        ownerTag.Remove(toRemove);
                        TagManagerWindow.OnTagOrphaned?.Invoke(toRemove);
                    }
                }
            }
        }

        public void DeleteTag()
        {
            var answer = EditorUtility.DisplayDialog("Delete Tag?", "Deleting this tag. Proceed?", "Continue", "Cancel");

            if(answer)
            {
                owner.OnTagRemoved?.Invoke(associatedTag);

                //Orphan all tags! (if group)
                var associatedGroup = associatedTag as LBSTagGroup;
                if(associatedGroup!=null)
                {
                    foreach(LBSTag tag in associatedGroup.Tags)
                    {
                        associatedGroup.Remove(tag);
                        TagManagerWindow.OnTagOrphaned(tag);
                    }
                }

                //Remove from owner! (Just in case)
                var ownerTag = owner.AssociatedTag as LBSTagGroup;
                var toRemove = associatedTag as LBSTag;
                if (ownerTag != null)
                {
                    if (ownerTag.Tags.Contains(toRemove))
                    {
                        ownerTag.Remove(toRemove);
                    }
                }


                var filePath = AssetDatabase.GetAssetPath(associatedTag);
                AssetDatabase.DeleteAsset(filePath);

                //...How do I actually make it delete stuff? lmao - Alice
            }
        }
        #endregion
    }

}
