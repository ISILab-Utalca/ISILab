using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor.CustomComponents;
using ISILab.LBS.Plugin.UI.Editor.Windows.TagManager;
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.Editor.UI.CustomComponents
{
    public class LBSTagListCreateTag : EditorWindow
    {
        #region FIELDS
        public enum objectType { Individual, Group };
        private objectType type;
        private string tagName = "";
        private LBSTagListGroup targetVisualGroup = null;
        private LBSTagGroup targetTagGroup;
        private LBSTagGroup.TagType targetLayerTag = LBSTagGroup.TagType.Element;
        #endregion

        #region VISUAL ELEMENTS
        private Label createNewLabel;
        private LBSCustomTextField nameField;
        private VisualElement groupContainer;
        private VisualElement typeContainer;
        private LBSCustomObjectField groupField;
        private LBSCustomEnumFlagField typeField;
        private Button createButton;
        private Button cancelButton;

        private VisualElement warningContainer;
        private Label warningLabel;
        #endregion

        #region PROPERTIES
        public LBSTagListGroup TargetVisualGroup
        {
            get => targetVisualGroup;
            set => targetVisualGroup = value;
        }

        public LBSTagGroup TargetTagGroup
        {
            get => targetTagGroup;
            set => targetTagGroup = value;
        }

        public objectType Type
        {
            get => type;
            set => type = value;
        }
        #endregion

        #region EVENTS
        #endregion

        #region CONSTRUCTORS
        public void CreateGUI()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSTagListCreateTag");
            visualTree.CloneTree(rootVisualElement);

            createNewLabel = rootVisualElement.Q<Label>("CreateNew");
            createNewLabel.text = "Create New " + (Type == objectType.Individual ? "Tag" : "Tag Group");

            nameField = rootVisualElement.Q<LBSCustomTextField>("NameField");
            nameField.RegisterValueChangedCallback(c => tagName = c.newValue);

            groupContainer = rootVisualElement.Q<VisualElement>("GroupContainer");
            typeContainer = rootVisualElement.Q<VisualElement>("TypeContainer");

            groupField = rootVisualElement.Q<LBSCustomObjectField>("GroupField");
            typeField = rootVisualElement.Q<LBSCustomEnumFlagField>("TypeField");
            //typeField.Init(LBSTagGroup.TagType.Structural);
            switch(type)
            {
                case objectType.Individual:
                    groupField.value = targetTagGroup;

                    groupContainer.style.visibility = Visibility.Visible;
                    groupContainer.style.display = DisplayStyle.Flex;

                    typeContainer.style.visibility = Visibility.Hidden;
                    typeContainer.style.display = DisplayStyle.None;

                    break;
                case objectType.Group:
                    groupField.value = null;

                    groupContainer.style.visibility = Visibility.Hidden;
                    groupContainer.style.display = DisplayStyle.None;

                    typeContainer.style.visibility = Visibility.Visible;
                    typeContainer.style.display = DisplayStyle.Flex;
                    break;
            }
            groupField.RegisterValueChangedCallback(c => targetTagGroup = c.newValue as LBSTagGroup);
            typeField.RegisterValueChangedCallback(c => targetLayerTag = (LBSTagGroup.TagType)c.newValue);

            cancelButton = rootVisualElement.Q<LBSCustomButton>("CancelButton");
            cancelButton.clicked += CloseWindow;

            createButton = rootVisualElement.Q<LBSCustomButton>("CreateButton");
            createButton.clicked += CreateTag;

            warningContainer = rootVisualElement.Q<VisualElement>("WarningContainer");
            warningLabel =  rootVisualElement.Q<Label>("WarningLabel");
        }
        #endregion

        #region METHODS
        public void CreateTag()
        {
            //Warnings
            if ((tagName == null) || (tagName == ""))
            {
                SetWarning("Can't save tag with no name.");
                return;
            }

            //Switch
            ScriptableObject tag = new();
            switch(Type)
            {
                case objectType.Individual:
                    if (TagManagerWindow.AllTags.Find(c => c.name == tagName) != null)
                    {
                        SetWarning("A tag with this name already exists.");
                        return;
                    }

                    var indivTag = CreateInstance<LBSTag>();
                    indivTag.name = tagName;
                    indivTag.label = tagName;

                    if (targetTagGroup != null)
                    {
                        targetTagGroup.Add(indivTag);
                    }

                    tag = indivTag;
                    TagManagerWindow.OnTagAdded?.Invoke(targetTagGroup, indivTag);
                    break;
                case objectType.Group:
                    if (TagManagerWindow.AllTagGroups.Find(c => c.name == tagName) != null)
                    {
                        SetWarning("A tag with this name already exists.");
                        return;
                    }

                    var groupTag = CreateInstance<LBSTagGroup>();
                    groupTag.name = tagName;
                    groupTag.type = targetLayerTag;

                    tag = groupTag;
                    TagManagerWindow.OnTagGroupAdded?.Invoke(groupTag);
                    break;
            }

            //Save as file!
            var settings = LBSSettings.Instance;
            var path = settings.paths.tagFolderPath;
            //Directory making
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            AssetDatabase.CreateAsset(tag, path + "/" + tagName + ".asset");
            AssetDatabase.SaveAssets();
            CloseWindow();
        }

        public void SetWarning(string warningText)
        {
            if((warningText==null)||(warningText==""))
            {
                warningContainer.style.visibility = Visibility.Hidden;
            } else
            {
                warningContainer.style.visibility = Visibility.Visible;
                warningLabel.text = warningText;
            }
        }

        public void ShowWindow()
        {
            LBSTagListCreateTag window = GetWindow<LBSTagListCreateTag>();
            Texture icon = AssetMacro.LoadAssetByGuid<Texture>("40d548834301ba14f96af3e1715add5f");
            window.minSize = new Vector2(350, 120); // use the Canvas Size of the uxml
            window.titleContent = new GUIContent("Create New Tag", icon);
        }

        public static void CloseWindow()
        {
            LBSTagListCreateTag window = GetWindow<LBSTagListCreateTag>();
            window.Close();
        }
        #endregion
    }

}
