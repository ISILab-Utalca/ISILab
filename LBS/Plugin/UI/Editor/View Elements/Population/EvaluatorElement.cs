

using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.View_Elements.Population.EvaluatorElement
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

        public event Action<EvaluatorElement> OnDelete;

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

        public EvaluatorElement() : base()
        {
            InitInternal();
        }

        public EvaluatorElement(string label, bool b1, bool b2, bool b3)
        {
            InitInternal();
            setEvaluatorElement(label, b1, b2, b3);
        }

        private void InitInternal()
        {
            GetVisualTreeForThis();

            evLabel = this.Q<LBSCustomLabel>("evName");

            evDeleteButton = this.Q<LBSCustomButton>("evDelete");
            evDeleteButton.RegisterCallback<ClickEvent>(DeleteEvaluatorElement);
            evConfigButton = this.Q<LBSCustomButton>("evOptions");
            evConfigButton.RegisterCallback<ClickEvent>(OpenEvaluatorConfig);


            interfaceIcon1 = this.Q<VisualElement>("InterfaceIcon1");
            interfaceIcon2 = this.Q<VisualElement>("InterfaceIcon2");
            interfaceIcon3 = this.Q<VisualElement>("InterfaceIcon3");

            interfaceBoolList = new List<bool>() { false, false, false };

            interfaceBoolListVisualElements = new List<VisualElement>
            {
                interfaceIcon1,
                interfaceIcon2,
                interfaceIcon3
            };
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

        private void DeleteEvaluatorElement(ClickEvent evt)
        {
            OnDelete?.Invoke(this);
        }

        private void OpenEvaluatorConfig(ClickEvent evt)
        {

        }
    }
}
