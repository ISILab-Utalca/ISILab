using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class QATagToggle : VisualElement
    {
        private Toggle toggle;
        private IntegerField quantity;

        public Toggle Toggle => toggle;
        public IntegerField Quantity => quantity;

        private const string UXML_NAME = "QATagToggle";
        private static VisualTreeAsset visualTree;

        public QATagToggle() 
        {
            visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>(UXML_NAME);
            if (visualTree != null) visualTree.CloneTree(this);
            else Debug.LogError($"[QATagToggle] No se encontró el UXML: {UXML_NAME}");

            toggle = this.Q<Toggle>("Toggle");
            quantity = this.Q<IntegerField>("IntegerField");
        }
    }
}
