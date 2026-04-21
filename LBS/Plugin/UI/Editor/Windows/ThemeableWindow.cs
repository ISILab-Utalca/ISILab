using System;
using ISILab.LBS.Plugin.Core.Settings;
using UnityEditor;

namespace ISILab.LBS.Plugin.UI.Editor.Windows
{
    public class ThemeableWindow: EditorWindow
    {
        public void CreateGUI()
        {
            
        }


        public virtual void ChangeTheme(LBSSettings.Interface.InterfaceTheme _newTheme)
        {
            switch (_newTheme)
            {
                case  LBSSettings.Interface.InterfaceTheme.Light:
                    rootVisualElement.ClearClassList();
                    rootVisualElement.AddToClassList("light");
                    //Repaint();
                    break;
                case  LBSSettings.Interface.InterfaceTheme.Dark:
                    rootVisualElement.ClearClassList();
                    rootVisualElement.AddToClassList("dark");
                    //Repaint();
                    break;
                case LBSSettings.Interface.InterfaceTheme.Alt:
                    rootVisualElement.ClearClassList();
                    rootVisualElement.AddToClassList("alt");
                    //Repaint();
                    break;
                case LBSSettings.Interface.InterfaceTheme.Darker:
                    rootVisualElement.ClearClassList();
                    rootVisualElement.AddToClassList("darker");
                    break;
                default:
                    break;
            }
        }
    }
}
