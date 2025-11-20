using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Assistants;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Editor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("Navigable Tags", typeof(WFCPresetsCharacteristic))]
    public class WFCPresetsCharacteristicEditor : LBSCustomEditor
    {
        public VisualElement content;

        private ListView presetsList;

        public WFCPresetsCharacteristicEditor(object target) : base(target)
        {
            CreateVisualElement();
            SetInfo(target);
        }

        public override void SetInfo(object paramTarget)
        {
            target = paramTarget;

            if (target is not WFCPresetsCharacteristic presets) return;

            presetsList.itemsSource = presets.Presets;
            presetsList.bindItem = (element, i) =>
            {
                var obj = element.Q<ObjectField>("Element");

                var asset = presetsList.itemsSource[i];
                obj.value = asset as WFCPreset;
            };
            presetsList.Rebuild();

            //content = new VisualElement();
            //Add(content);
            //content.Add(new ListView(presets.Presets));
        }

        protected override VisualElement CreateVisualElement()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("WFCPresetsCharacteristicEditor");
            visualTree.CloneTree(this);

            presetsList = this.Q<ListView>("PresetsList");

            return this;
        }
    }
}

