using ISILab.Commons.Utility.Editor;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager
{
    public class BundleManagerWarning : VisualElement
    {
        private readonly Label _warningContent;

        public BundleManagerWarning()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("BundleManagerWarning");
            visualTree.CloneTree(this);
            
            _warningContent = this.Q<Label>("WarningContent");
        }

        public void SetWarningContent(string warningContent)
        {
            _warningContent.text = warningContent;
        }
    }
}
