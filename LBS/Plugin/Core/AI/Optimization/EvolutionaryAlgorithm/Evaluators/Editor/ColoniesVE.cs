using ISILab.AI.Categorization;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("Colonies", typeof(Colonies))]
    public class ColoniesVE : LBSCustomEditor
    {
		LBSCustomButton configurationButton;

		public ColoniesVE(object target) : base(target)
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

			static void ShowConfiguration() => Selection.activeObject = Colonies.config;

			return this;
		}

		public override void SetInfo(object paramTarget) { }
    }
}