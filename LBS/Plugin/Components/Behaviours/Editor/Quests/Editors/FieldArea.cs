using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class FieldArea : GrammarFieldEditor
    {
        public FieldArea(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("FieldArea");
            visualTree.CloneTree(content);

            var pv2 = this.Q<PickerVector2Int>();
            pv2.OnAreaChange = (newArea) =>
            {
                (target as GrammarArea).SetValue(newArea);
            };

            UnityEngine.Rect area = (target as GrammarArea).value;
            if (area != null) pv2.SetArea(area);

            return this;
        }
    }
}