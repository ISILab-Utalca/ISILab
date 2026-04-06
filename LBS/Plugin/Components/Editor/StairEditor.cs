using ISILab.LBS.Editor;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Internal;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Modules;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("Stair", typeof(LBSStair))]
    public class StairEditor : LBSCustomEditor
    {
        private LBSStair _target;

        private TextField nameField;
        private ColorField colorField;
        private ObjectField objectField;

        private ListView bundleList;
        private List<string> bundlesRef;

        public StairEditor()
        {
            CreateVisualElement();
        }

        public override void SetInfo(object paramTarget)
        {
            // Set referenced Zone
            _target = paramTarget as LBSStair;

            // Get bundles
            var bundles = LBSAssetsStorage.Instance.Get<Bundle>();

            // Set basic values
            var pos = _target.Positions[0];
            nameField.value = $"Stair ({pos.x} , {pos.y})";
            colorField.value = _target.Color;

            if (_target.Styles.Count > 0)
            {
                foreach (var bundle in bundles)
                {
                    if (bundle.name == _target.Styles[0])
                    {
                        objectField.value = bundle;
                        break;
                    }
                }
            }

            bundlesRef = _target.Styles;
        }

        protected override VisualElement CreateVisualElement()
        {
            // NameField
            nameField = new TextField("Name");
            nameField.SetEnabled(false);
            Add(nameField);

            // ColorField
            colorField = new ColorField("Color");
            colorField.RegisterCallback<ChangeEvent<Color>>(evt =>
            {
                _target.Color = evt.newValue;

            });
            Add(colorField);

            // ObjectField (Bundle)
            objectField = new ObjectField("Inside Style");
            objectField.objectType = typeof(Bundle);
            objectField.RegisterValueChangedCallback(v =>
            {
                var bundle = v.newValue as Bundle;
                _target.Styles = new List<string>() { bundle.name };
            });
            Add(objectField);

            return this;

        }

        private VisualElement MakeItem()
        {
            var field = new ObjectField();
            field.objectType = typeof(Bundle);
            return field;
        }

        private void BindItem(VisualElement ve, int index)
        {
            var field = ve as ObjectField;

            var bundles = LBSAssetsStorage.Instance.Get<Bundle>();
            bundles = bundles.Where(b => b.Name == bundlesRef[index]).ToList();

            field.value = bundles[index];
        }

    }
}