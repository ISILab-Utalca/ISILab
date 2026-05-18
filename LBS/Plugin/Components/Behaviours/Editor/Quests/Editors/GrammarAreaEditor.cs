using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [GrammarFieldEditor(typeof(GrammarArea))]
    public class GrammarAreaEditor : GrammarFieldEditor
    {
        public GrammarAreaEditor(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("GrammarAreaEditor");
            visualTree.CloneTree(content);

            var pv2 = this.Q<PickerVector2Int>();
            pv2.OnAreaChange = (newArea) =>
            {
                (target as GrammarArea).SetValue(newArea);
            };

            (target as GrammarField).Refresh = () =>
            {
                UnityEngine.Rect area = (target as GrammarArea).value;
                pv2._areaView.SetValueWithoutNotify(area);
            };
            
            return this;
        }
    }
}