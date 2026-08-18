using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Editor;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("WFC Rulesets", typeof(WFCRulesetsCharacteristic))]
    public class WFCRulesetsCharacteristicEditor : LBSCustomEditor
    {
        public VisualElement content;

        private ListView rulesetsList;

        public WFCRulesetsCharacteristicEditor(object target) : base(target)
        {
            CreateVisualElement();
            SetInfo(target);
        }

        public override void SetInfo(object paramTarget)
        {
            target = paramTarget;

            if (target is not WFCRulesetsCharacteristic rulesets) return;

            rulesetsList.itemsSource = rulesets.Rulesets;
            rulesetsList.bindItem = (element, i) =>
            {
                var obj = element.Q<ObjectField>("Element");

                var asset = rulesetsList.itemsSource[i];
                obj.value = asset as WFCRuleset;
            };
            rulesetsList.Rebuild();
        }

        protected override VisualElement CreateVisualElement()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("WFCRulesetsCharacteristicEditor");
            visualTree.CloneTree(this);

            rulesetsList = this.Q<ListView>("RulesetsList");

            return this;
        }
    }
}

