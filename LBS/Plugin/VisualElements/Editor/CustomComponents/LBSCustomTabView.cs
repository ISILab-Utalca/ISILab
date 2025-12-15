using System.Collections.Generic;
using UnityEngine.UIElements;


//namespace ISILab.LBS.Plugin.VisualElements.Editor.CustomComponents
//{
    [UxmlElement]
    public partial class LBSCustomTabView : TabView
    {

        private bool displayTabs = true;

        [UxmlAttribute]
        public bool DisplayTabs
        {
            get => displayTabs;
            set
            {
                displayTabs = value;
                schedule.Execute(() => SetTabsVisibility(value));
            }
        }


        public LBSCustomTabView() : base()
        {
            AddToClassList("lbs-tab-view");
        }

        private void SetTabsVisibility(bool _newState)
        {
            List<VisualElement> headers = this.Query<VisualElement>(name: "unity-tab__header").ToList();
            foreach (VisualElement tab in headers)
            {
                tab.style.display = _newState ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
//}

