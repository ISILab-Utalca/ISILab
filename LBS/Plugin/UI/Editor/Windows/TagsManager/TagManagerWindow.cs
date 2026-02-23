using ISILab.DevTools.Macros;
using ISILab.LBS.Plugin.UI.Editor.Windows;
using UnityEditor;
using UnityEngine;



public class TagManagerWindow :ThemeableWindow
{
    [MenuItem("Window/ISILab/Tag Manager", priority = 2)]
    public static void ShowWindow()
    {
        TagManagerWindow window = GetWindow<TagManagerWindow>();
        Texture icon = AssetMacro.LoadAssetByGuid<Texture>("6351057aa17189c44902075c0b9353fd");
        window.titleContent = new GUIContent("Tag Manager", icon);
    }
}
