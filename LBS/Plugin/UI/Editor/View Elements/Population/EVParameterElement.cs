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
        private LBSCustomLabel paramLabel;

        private bool canBeDeleted = false;
        private LBSCustomButton paramDeleteButton;
        public event Action<EVParameterElement> OnDelete;

        public string ParamLabelString
        {
            get => paramLabel.text;
            private set => paramLabel.text = value;
        }
        public bool CanBeDeleted
        {
            get => canBeDeleted;
            set => canBeDeleted = value;
        }
        
        public EVParameterElement() : base()
        {
            Initialize();
        }
        public EVParameterElement(string label, bool b) : base()
        {
            Initialize(label, b);
        }

        public void Initialize(string label = "", bool b = false)
        {
            GetVisualTreeForThis();

            paramLabel = this.Q<LBSCustomLabel>("paramName");
            paramDeleteButton = this.Q<LBSCustomButton>("paramDelete");
            paramDeleteButton.RegisterCallback<ClickEvent>(DeleteParameterElement);

            setParameterElement(label, b);
        }

        public void setParameterElement(string label, bool b)
        {
            ParamLabelString = label;
            canBeDeleted = b;
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
