using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.View_Elements.Population.EVParameterElement
{
    /// <summary>
    /// Visual element used to show parameters in a list.
    /// </summary>
    [UxmlElement]
    public partial class EVParameterElement : LBSComplexVisualElement
    {
        private string paramlabelString;
        private LBSCustomLabel paramNameLabel;
        private LBSCustomLabel paramTypeLabel;
        private LBSCustomLabel paramIValueLabel;
        private VisualElement verticalDivider1;
        private VisualElement verticalDivider2;
        //private VisualElement verticalDivider3;

        private bool canBeDeleted = false;
        private LBSCustomButton paramDeleteButton;

        public event Action<EVParameterElement> OnDelete;

        public string ParamLabelString
        {
            get => paramNameLabel.text;
            private set => paramNameLabel.text = value;
        }
        public bool CanBeDeleted
        {
            get => canBeDeleted;
            set {
                canBeDeleted = value;
                if (!value)
                {
                    paramDeleteButton.visible = false;
                    verticalDivider1.style.display = DisplayStyle.None;
                    verticalDivider2.style.display = DisplayStyle.None;
                }
            }
        }
        
        public EVParameterElement() : base()
        {
            Initialize();
        }
        public EVParameterElement(string label, bool b, string type, string iValue) : base()
        {
            Initialize(label, b, type, iValue);
        }

        public void Initialize(string label = "", bool b = true, string type="", string iValue="")
        {
            GetVisualTreeForThis();

            paramNameLabel = this.Q<LBSCustomLabel>("paramName");
            paramTypeLabel = this.Q<LBSCustomLabel>("paramType");
            paramIValueLabel = this.Q<LBSCustomLabel>("paramIValue");
            paramDeleteButton = this.Q<LBSCustomButton>("paramDelete");
            paramDeleteButton.RegisterCallback<ClickEvent>(DeleteParameterElement);
            verticalDivider1 = this.Q<VisualElement>("VerticalDivider1");
            verticalDivider2 = this.Q<VisualElement>("VerticalDivider2");
            //verticalDivider3 = this.Q<VisualElement>("VerticalDivider3");

            setParameterElement(label, b, type, iValue);
        }

        public void setParameterElement(string label, bool b, string type, string iValue)
        {
            ParamLabelString = label;
            paramTypeLabel.text = type;
            paramIValueLabel.text = iValue;
            CanBeDeleted = b;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        private void DeleteParameterElement(ClickEvent evt)
        {
            if(canBeDeleted)
                OnDelete?.Invoke(this);
        }
    }
}
