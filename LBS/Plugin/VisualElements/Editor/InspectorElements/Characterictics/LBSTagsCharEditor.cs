using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.Internal;
using ISILab.LBS.Plugin.Internal;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Editor
{
    [LBSCustomEditor("Tag identifier", typeof(LBSTagsCharacteristic))]
    public class LBSTagsCharEditor : LBSCustomEditor
    {
        private VisualElement root;
        private VisualElement listContainer;
        private Button addButton;

        public LBSTagsCharEditor() => CreateUI();
        public LBSTagsCharEditor(object target) : base(target) => CreateUI();

        public override void SetInfo(object paramTarget)
        {
            this.target = paramTarget;
            RefreshList();
        }

        protected override VisualElement CreateVisualElement() => root;

        private void CreateUI()
        {
            root = new VisualElement();
            listContainer = new VisualElement();
            root.Add(listContainer);

            addButton = new Button(() => AddEntry()) { text = "Add Tag" };
            addButton.style.marginTop = 6;
            root.Add(addButton);
        }

        private void RefreshList()
        {
            listContainer.Clear();

            var tc = target as LBSTagsCharacteristic;
            if (tc == null) return;

            var allTags = LBSAssetsStorage.Instance.Get<LBSTag>();

            foreach (var entry in tc.Tags)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;

                var dropdown = new DropdownField();
                dropdown.label = "Tag:";
                dropdown.choices = allTags.Select(t => t.Label).ToList();

                if (entry.Value != null)
                    dropdown.SetValueWithoutNotify(entry.Value.Label);

                dropdown.RegisterValueChangedCallback(e =>
                {
                    var selected = allTags.FirstOrDefault(t => t.Label == e.newValue);
                    entry.Value = selected;
                });

                var removeBtn = new Button(() =>
                {
                    tc.RemoveTag(entry.Value);
                    RefreshList();
                })
                {
                    text = "X"
                };
                removeBtn.style.marginLeft = 4;
                removeBtn.style.width = 20;

                row.Add(dropdown);
                row.Add(removeBtn);
                listContainer.Add(row);
            }
        }

        private void AddEntry()
        {
            var tc = target as LBSTagsCharacteristic;
            if (tc == null) return;

            var allTags = LBSAssetsStorage.Instance.Get<LBSTag>();
            if (allTags.Count == 0) return;

            // Add the first available tag by default
            tc.AddTag(allTags[0]);
            RefreshList();
        }
    }
}
