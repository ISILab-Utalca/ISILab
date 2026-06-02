

using ISILab.Extensions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

namespace ISILab.LBS.CustomComponents
{
    
    [UxmlElement]
    public partial class LBSCustomPainter: VisualElement
    {
        
        private Color fillColor;
        private Color strokeColor;
        private float lineWidth;


        [UxmlAttribute]
        public Color StrokeColor
        {
            get => strokeColor;
            set
            {
                strokeColor = value;
                MarkDirtyRepaint();
            }
        }


        [UxmlAttribute]
        public Color FillColor
        {
            get => fillColor;
            set
            {
                fillColor = value;
                MarkDirtyRepaint();
            }
        }


        [UxmlAttribute]
        public float LineWidth
        {
            get => lineWidth;
            set
            {
                lineWidth = value;
                MarkDirtyRepaint();
            }
        }

        public Vector2 MinPos { get; set; } = Vector2.zero;
        public Vector2 MaxPos { get; set; } = Vector2.zero;

        public LBSCustomPainter() : base()
        {
            style.position = Position.Absolute;

            MaxPos = new Vector2(100,100);

            fillColor = Color.white;
            strokeColor = Color.black;

            generateVisualContent += DrawContent;
            MarkDirtyRepaint();
        }


        private void DrawContent(MeshGenerationContext mgc)
        {
            var painter2D = mgc.painter2D;

            painter2D.DrawSelectionBox(MinPos, MaxPos, FillColor, strokeColor, 1);
        }

    }
}
