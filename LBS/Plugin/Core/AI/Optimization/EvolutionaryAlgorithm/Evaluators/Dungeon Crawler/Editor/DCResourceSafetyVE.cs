using ISILab.AI.Categorization;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("DCResourceSafety", typeof(DCResourceSafety))]
    public class DCResourceSafetyVE : LBSCustomEditor
    {
        LBSCustomButton configurationButton;

        public DCResourceSafetyVE(object target) : base(target)
	    {
	        CreateVisualElement();
            SetInfo(target);
	    }

	    protected override VisualElement CreateVisualElement()
        {
            configurationButton = new LBSCustomButton();
            configurationButton.style.height = 30;
            configurationButton.text = "Advanced configuration";
            configurationButton.clicked -= ShowConfiguration;
            configurationButton.clicked += ShowConfiguration;
            Add(configurationButton);

            static void ShowConfiguration() => Selection.activeObject = DCResourceSafety.config;

            return this;
        }

        public override void SetInfo(object paramTarget) { }
    }
}