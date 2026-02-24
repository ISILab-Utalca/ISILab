using ISILab.Commons.Utility.Editor;
using UnityEngine;
using UnityEngine.UIElements;
namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomLabelItem : Label
    {
        private LBSCustomLabel labelTab, labelL, labelR;
        private string stringTab, stringL, stringR;

        public LBSCustomLabelItem():base()
        {
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSCustomLabelItem");
            visualTree.CloneTree(this);

            labelTab = this.Q<LBSCustomLabel>("textTab");
            labelL = this.Q<LBSCustomLabel>("textL");
            labelR = this.Q<LBSCustomLabel>("textR");

            if (labelTab != null) labelTab.text = stringTab;
            if (labelL != null) labelL.text = stringL;
            if (labelR != null) labelR.text = stringR;

            labelTab?.AddToClassList("lbs-show-white-spaces");
        }
        
        [UxmlAttribute]
        public string TextTab
        {
            get { return labelTab.text; }
            set
            {
                this.labelTab.text = value;
            }
        }
        [UxmlAttribute]
        public string TextL
        {
            get { return labelL.text; }
            set
            {
                this.labelL.text = value;
            }
        }
        [UxmlAttribute]
        public string TextR
        {
            get { return labelR.text; }
            set
            {
                this.labelR.text = value;
            }
        }
        /*
        public void SetAlltext(string tab, string l, string r)
        {
            if (tab != null) TextTab = tab;
            if (l != null) TextL = l;
            if (r != null) TextR = r;
        }
        */
    }
}