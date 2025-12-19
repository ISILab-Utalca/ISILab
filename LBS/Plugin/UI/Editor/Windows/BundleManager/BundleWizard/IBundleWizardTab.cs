namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard
{
    public interface IBundleWizardTab
    {
        public BundleBuilder Builder { get; set; }
        public void Init();
        public void Step();
        public void StepBack();
        public void Revert();
    }
}
