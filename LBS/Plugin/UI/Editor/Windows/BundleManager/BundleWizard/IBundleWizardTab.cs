namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard
{
    /// <summary>
    /// Interface holding a reference to the <see cref="BundleBuilder"></see> and useful methods for the Wizard flow.
    /// </summary>
    public interface IBundleWizardTab
    {
        public BundleBuilder Builder { get; set; }

        /// <summary>
        /// Called when the tab is <b>showed</b> after using the <b>'Next'</b> button. <br />
        /// Meant to initialize the current step after completing the previous one. 
        /// </summary>
        public void Init();
        /// <summary>
        /// Called when the tab is <b>hidden</b> after using either the <b>'Next'</b> button or the <b>'OK'</b> button. <br />
        /// Meant to pass the entered data from the current step to the <see cref="BundleBuilder"></see> before continuing to the next step.
        /// </summary>
        public void Step();
        /// <summary>
        /// Called when the tab is <b>showed</b> after using the <b>'Back'</b> button. <br />
        /// Serves as a light initialization after cancelling the later step.
        /// </summary>
        public void StepBack();
        /// <summary>
        /// Called when the tab is <b>hidden</b> after using the <b>'Back'</b> button, or when the pop-up is closed. <br />
        /// Meant to clean the current step data and the Builder data before returning to the previous step.
        /// </summary>
        public void Revert();
    }
}
