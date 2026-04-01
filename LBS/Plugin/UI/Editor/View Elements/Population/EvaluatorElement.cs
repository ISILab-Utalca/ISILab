

using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard
{
    /// <summary>
    /// Visual element used to show evaluators in a list.
    /// </summary>
    [UxmlElement]
    public partial class EvaluatorElement : LBSComplexVisualElement
    {
        private LBSCustomLabel evLabel;
        private LBSCustomButton evConfigButton;
        private LBSCustomButton evDeleteButton;
        private VisualElement interfaceIcon1;
        private VisualElement interfaceIcon2;
        private VisualElement interfaceIcon3;

        private string evlabelString;
        private List<bool> interfaceBoolList;
        private List<VisualElement> interfaceBoolListVisualElements;

        public string EvLabelString
        {
            get => evLabel.text;
            private set => evLabel.text = value;
        }

        public List<bool> InterfaceBoolList
        {
            get => interfaceBoolList;
            private set => interfaceBoolList = value;
        }

        //public string QuestionTooltipText
        //{
        //    get => questionTooltipText;
        //    private set
        //    {
        //        questionTooltipText = value;
        //        QuestionTooltip.tooltip = value;
        //    }
        //}

        //public int Index
        //{
        //    get => index;
        //    set 
        //    {
        //        if(index != value) 
        //            Debug.LogWarning("Element " + CharLabel.text + " changed index: " + index + " -> " + value);
        //        index = value;
        //    }
        //}

        public EvaluatorElement() : base()
        {
            GetVisualTreeForThis();

            evLabel         = this.Q<LBSCustomLabel>();

            evConfigButton  = this.Q<LBSCustomButton>();
            evDeleteButton  = this.Q<LBSCustomButton>();

            interfaceIcon1  = this.Q<VisualElement>();
            interfaceIcon2  = this.Q<VisualElement>();
            interfaceIcon3  = this.Q<VisualElement>();

            interfaceBoolList = new List<bool>() { false,false,false };

            interfaceBoolListVisualElements = new List<VisualElement>();
            interfaceBoolListVisualElements.Add(interfaceIcon1);
            interfaceBoolListVisualElements.Add(interfaceIcon2);
            interfaceBoolListVisualElements.Add(interfaceIcon3);
        }
        public void setEvaluatorElement(string label, bool b1, bool b2, bool b3)
        {
            EvLabelString = label;
            setInterfaceBooleanList(b1, b2, b3);
        }
        public void setInterfaceBooleanList(bool b1, bool b2, bool b3)
        {
            interfaceBoolList[0] = b1;
            interfaceBoolList[1] = b2;
            interfaceBoolList[2] = b3;
            setInterfaceIconVisibility(b1, b2, b3);
        }
        public void setInterfaceBooleanByIndex(int i, bool b)
        {
            interfaceBoolList[i] = b;
            setInterfaceIconVisibilitybyIndex(i, b);
        }
        public void setInterfaceIconVisibility(bool b1, bool b2, bool b3)
        {
            interfaceBoolListVisualElements[0].visible = b1;
            interfaceBoolListVisualElements[1].visible = b2;
            interfaceBoolListVisualElements[2].visible = b3;
        }
        public void setInterfaceIconVisibilitybyIndex(int i, bool b)
        {
            interfaceBoolListVisualElements[i].visible = b;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
