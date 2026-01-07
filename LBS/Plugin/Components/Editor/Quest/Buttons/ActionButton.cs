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
        public LBSToolbarToggle _toggle;

        public  ActionButton()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("ActionButton");
            visualTree.CloneTree(this);
            _toggle = this.Q<LBSToolbarToggle>("ActionToggle");
        }

        public ActionButton(string text, Action action) : this()
        {
            _toggle.label = char.ToUpper(text[0]) + text.Substring(1);
            _toggle.RegisterCallback<ClickEvent>(evt =>
            {
                action?.Invoke();
                _toggle.SetValueWithoutNotify(true);
            });
        }
    }
}