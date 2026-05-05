using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [GrammarFieldEditor(typeof(GrammarColor))]
    public class GrammarColorEditor : GrammarFieldEditor
    {
        public GrammarColorEditor(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("GrammarColorEditor");
            visualTree.CloneTree(content);

            this.Q<ColorField>().RegisterValueChangedCallback(evt =>
            {
                SetTargetValue(evt);
            });

            (target as GrammarField).Refresh = () =>
            {
                this.Q<ColorField>().SetValueWithoutNotify(GetTargetValue<Color>());
            };
              
            return this;
        }
    }
}