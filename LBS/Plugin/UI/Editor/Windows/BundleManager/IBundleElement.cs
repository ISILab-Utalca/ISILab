using ISILab.LBS.Plugin.Components.Bundles;
using UnityEngine.UIElements;
using ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager
{
    /// <summary>
    /// Interface meant to be implemented by a visual element of a list view from <see cref="BundleManagerWindow"/>, representing a <see cref="Bundle"/>.
    /// <para>
    /// Current implementations: <see cref="BundleManagerElement"/>, <see cref="BundleWizardElement"/>
    /// </para>
    /// </summary>
    public interface IBundleElement
    {
        public Bundle BundleRef { get; set; }
        public ListView ListRef { get; set; }

        /// <summary>
        /// Sets references for the represented bundle and the list view it belongs to.
        /// </summary>
        /// <param name="bundle"> The bundle represented by this visual element. </param>
        /// <param name="list"> The list containing the visual element. </param>
        /// <param name="boolParam"> Additional boolean parameter for specific implementations. </param>
        public void SetBundleReference(Bundle bundle, ListView list, bool boolParam);
        /// <summary>
        /// Identifies an icon by its name and sets it as visible or not visible.
        /// </summary>
        /// <param name="iconName"></param>
        /// <param name="display"></param>
        public void SetIconDisplay(string iconName, bool display);
    }
}

