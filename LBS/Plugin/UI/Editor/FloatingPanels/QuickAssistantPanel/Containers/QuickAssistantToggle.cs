using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class QuickAssistantToggle : VisualElement
    {
        private Toggle toggle;
        private IntegerField quantity;


        public string Label { get => toggle.label; set { toggle.label = value; } }
        public bool Value { get => toggle.value; set { toggle.value = value; } }
        public int Quantity { get => quantity.value; set { quantity.value = value; } }

        private const string UXML_NAME = "QuickAssistantToggle";
        private static VisualTreeAsset visualTree;

        public QuickAssistantToggle() 
        {
            visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>(UXML_NAME);
            if (visualTree != null) visualTree.CloneTree(this);
            else Debug.LogError($"[QuickAssistantToggle] No se encontró el UXML: {UXML_NAME}");

            toggle = this.Q<Toggle>("QAToggle");
            quantity = this.Q<IntegerField>("QuantityField");
        }
    }
}
