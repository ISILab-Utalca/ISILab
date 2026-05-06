using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomIntField: IntegerField
    {
        private VectorImage typeIcon;
        private VectorImage addIcon;
        private VectorImage minusIcon;

        private Button addButton;
        private Button minusButton;
        private VisualElement iconVisualElement;

        [UxmlAttribute]
        public int Min { get; set; } = int.MinValue;

        [UxmlAttribute]
        public int Max { get; set; } = int.MaxValue;

        [UxmlAttribute]
        public VectorImage TypeIcon
        {
            get => typeIcon;
            set => typeIcon = value;
        }
        
        public LBSCustomIntField() : base()
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
                value = Mathf.Clamp(value + 1, Min, Max);
            });
            
            minusButton.RegisterCallback<ClickEvent>((evt) =>
            {
                value = Mathf.Clamp(value - 1, Min, Max);
            });
        }
    }
}


