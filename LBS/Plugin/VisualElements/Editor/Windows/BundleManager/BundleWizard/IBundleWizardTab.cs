public interface IBundleWizardTab
{
    public BundleBuilder Builder { get; set; }
    public void Init();

    public void Step();

    public void Revert();
}