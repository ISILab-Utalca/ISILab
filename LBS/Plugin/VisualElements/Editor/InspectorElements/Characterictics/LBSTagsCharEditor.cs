using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Internal;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Editor
{
    [LBSCustomEditor("Tag identifier", typeof(LBSTagsCharacteristic))]
    public class LBSTagsCharEditor : LBSCustomEditor
    {
        private VisualElement root;
        private DropdownField dropdownField;
        private ListView listView;

        private string LBSTagIcon = "d6f94a68988be8b45894b9f0e677e8d1";

        private LBSTagsCharacteristic TC => target as LBSTagsCharacteristic;

        public LBSTagsCharEditor() { }

        public LBSTagsCharEditor(object target) : base(target)
        {
            CreateVisualElement();
            SetInfo(target);
        }

        public override void SetInfo(object paramTarget)
        {
            target = paramTarget;
            var tc = TC;
            if (tc == null) return;

            listView.itemsSource = tc.TagEntries;
            listView.RefreshItems();
        }

        protected override VisualElement CreateVisualElement()
        {
            listView = new ListView();
            listView.showAddRemoveFooter = true;
            listView.selectionType = SelectionType.Single;
            listView.fixedItemHeight = 24;

            listView.makeItem = () =>
            {
                LBSCustomObjectField objectField = new LBSCustomObjectField();
                objectField.objectType = typeof(LBSTag);
                objectField.style.flexGrow = 1;
  
                return objectField;
            };

            listView.bindItem = (ve, index) =>
            {
                LBSTagsCharacteristic tc = TC;
                if (index < 0 || index >= tc.TagEntries.Count) return;

                var entry = tc.TagEntries[index];
                var field = ve as ObjectField;

                // Remove previous callback to avoid duplicates
                field.UnregisterValueChangedCallback(OnChanged);

                field.SetValueWithoutNotify(entry.Value);

                // Register again safely
                field.RegisterValueChangedCallback(OnChanged);

                void OnChanged(ChangeEvent<Object> evt)
                {
                    entry.Value = evt.newValue as LBSTag;
                    entry.UpdateInfo();
                    EditorUtility.SetDirty(TC.Owner);
                }
            };


            listView.itemsSource = TC.TagEntries;

            listView.onAdd = (list) =>
            {
                TC.TagEntries.Add(new TagCharacteristicEntry());
                EditorUtility.SetDirty(TC.Owner);
                listView.Rebuild();
            };

            listView.onRemove = (list) =>
            {
                int index = listView.selectedIndex;
                if (index >= 0 && index < TC.TagEntries.Count)
                {
                    TC.TagEntries.RemoveAt(index);
                    EditorUtility.SetDirty(TC.Owner);
                    listView.Rebuild();
                }
            };


            this.Add(listView);
            return this;
        }

    }
}
