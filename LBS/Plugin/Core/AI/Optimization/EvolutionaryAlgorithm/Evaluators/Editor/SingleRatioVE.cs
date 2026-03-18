using ISILab.AI.Categorization;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("SingleRatio", typeof(SingleRatio))]
    public class SingleRatioVE : LBSCustomEditor
    {
		LBSCustomButton configurationButton;

		public SingleRatioVE(object target) : base(target)
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

			static void ShowConfiguration() => EvaluatorConfigurationWindow.Create(SingleRatio.config);


            return this;
		}

		public override void SetInfo(object paramTarget) { }
    }
}