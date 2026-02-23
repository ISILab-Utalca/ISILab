using System;
using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Plugin.UI.Editor.Windows;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class TagManagerWindow :ThemeableWindow
{
    
    
    public static TagManagerWindow Instance { get; private set; }
    
    
    [MenuItem("Window/ISILab/Tag Manager", priority = 2)]
    public static void ShowWindow()
    {
        TagManagerWindow window = GetWindow<TagManagerWindow>();
        Texture icon = AssetMacro.LoadAssetByGuid<Texture>("40d548834301ba14f96af3e1715add5f");
        window.titleContent = new GUIContent("Tag Manager", icon);
    }

    
    //Singleton part
    private void OnEnable()
    {
        Instance = this;
    }

    private void CreateGUI()
    {
        VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("TagManagerWindow");
        visualTree.CloneTree(rootVisualElement);
    }
    
    private void OnDisable()
    {
        Instance = null;
    }
}
