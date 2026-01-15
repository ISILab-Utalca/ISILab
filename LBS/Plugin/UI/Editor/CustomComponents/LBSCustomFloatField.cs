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

        private Button addButton;
        private Button minusButton;
        private VisualElement iconVisualElement;

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

            addButton.RegisterCallback<ClickEvent>((evt) =>
            {
                value = Mathf.Clamp(value + 0.01f, Min, Max);
            });
            
            minusButton.RegisterCallback<ClickEvent>((evt) =>
            {
                value = Mathf.Clamp(value + 0.01f, Min, Max);
            });
        }
    }
}


