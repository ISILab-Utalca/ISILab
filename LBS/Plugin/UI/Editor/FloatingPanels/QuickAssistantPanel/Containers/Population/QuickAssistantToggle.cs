using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class QuickAssistantToggle : VisualElement
    {
        private Toggle _toggle;
        private IntegerField _quantity;
        private Action<bool> _toggleAction = null;

        public string Label { get => _toggle.label; set { _toggle.label = value; } }
        public bool Value { get => _toggle.value; set { _toggle.value = value; } }
        public int Quantity { get => _quantity.value; set { _quantity.value = value; } }

        private const string UXML_NAME = "QuickAssistantToggle";
        private static VisualTreeAsset visualTree;

        public QuickAssistantToggle() 
        {
            visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>(UXML_NAME);
            if (visualTree != null) visualTree.CloneTree(this);
            else Debug.LogError($"[QuickAssistantToggle] No se encontró el UXML: {UXML_NAME}");

            _toggle = this.Q<Toggle>("QAToggle");
            _toggle.RegisterValueChangedCallback(evt =>
            {
                _toggleAction(_toggle.value);
            });
                
            _quantity = this.Q<IntegerField>("QuantityField");
        }

        public void SetToggleAction(Action<bool> toggleAction)
        {
            _toggleAction = toggleAction;
        }

    }
}
