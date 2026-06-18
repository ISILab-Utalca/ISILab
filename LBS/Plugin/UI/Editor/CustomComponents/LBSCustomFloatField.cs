using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomFloatField: FloatField
    {
        private VectorImage typeIcon;
        private VectorImage addIcon;
        private VectorImage minusIcon;

        private bool displayAdd;
        private bool displayMinus;

        private Button addButton;
        private Button minusButton;
        private VisualElement iconVisualElement;

        [UxmlAttribute]
        public bool DisplayAdd
        {
            get => displayAdd;
            set
            {
                displayAdd = value;
                if (addButton != null) 
                    addButton.style.display = displayAdd 
                        ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }


        [UxmlAttribute]
        public bool DisplayMinus
        {
            get => displayMinus;
            set
            {
                displayMinus = value;
                if (minusButton != null)
                    minusButton.style.display = displayMinus 
                        ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        [UxmlAttribute]
        public float Min { get; set; } = float.NegativeInfinity;

        [UxmlAttribute]
        public float Max { get; set; } = float.PositiveInfinity;

        [UxmlAttribute]
        public VectorImage TypeIcon
        {
            get => typeIcon;
            set => typeIcon = value;
        }
        
        public LBSCustomFloatField() : base()
        {
            
            addButton = new Button() { text = "+" };
            minusButton = new Button() { text = "-" };
            iconVisualElement = new VisualElement();

            minusButton.AddToClassList("minusButton");
            this.Add(minusButton);
            addButton.AddToClassList("addButton");
            this.Add(addButton);

            AddToClassList("lbs-input-field-float");

            addButton.RegisterCallback<ClickEvent>((evt) =>
            {
                value = Mathf.Clamp(value + 0.01f, Min, Max);
            });
            
            minusButton.RegisterCallback<ClickEvent>((evt) =>
            {
                value = Mathf.Clamp(value + 0.01f, Min, Max);
            });

            DisplayMinus = displayMinus;
            DisplayAdd = displayAdd;
        }
    }
}


