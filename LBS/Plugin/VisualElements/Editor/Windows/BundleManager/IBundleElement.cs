using LBS.Bundles;
using UnityEngine.UIElements;

public interface IBundleElement
{
    public Bundle BundleRef { get; set; }
    public ListView ListRef { get; set; }

    public void SetBundleReference(Bundle bundle, ListView list, bool boolParam);
    public void SetIconDisplay(string iconName, bool display);
}
