using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Core.Settings;
using System;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{

    [UxmlElement]
    public partial class ActionButton : VisualElement
    {

        UnityEngine.Color HighlightColor = LBSSettings.Instance.view.newToolkitSelected;
        UnityEngine.Color NormalColor = LBSSettings.Instance.view.toolkitNormal;

        static ActionButton selectedButton;

        string ActionText;
        private  Button _button;

        public  ActionButton()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("ActionButton");
            visualTree.CloneTree(this);
            _button = this.Q<LBSCustomButton>("Button");
        }

        public ActionButton(string text, Action action) : this()
        {
           
            ActionText = text;
            _button.text = char.ToUpper(text[0]) + text.Substring(1);
            _button.clicked += action;
            _button.clicked += () =>
            {
                SetHighlight(true);
                selectedButton = this;
            };
        }

        public void SetHighlight(bool isSelected)
        {
   
            if (selectedButton != null && selectedButton != this)
            {
                selectedButton._button.SetBackgroundColor(NormalColor);
            }

            var color = isSelected ? HighlightColor : NormalColor;
            _button.SetBackgroundColor(color);

            selectedButton = isSelected ? this : null;
        }

    }
}