

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
        
        protected Color fillColor;
        protected Color strokeColor;
        protected int lineWidth;


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
        public int LineWidth
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

            lineWidth = 1;
            fillColor = Color.white;
            strokeColor = Color.black;

            generateVisualContent += DrawContent;
            MarkDirtyRepaint();
        }



        protected virtual void DrawContent(MeshGenerationContext mgc) { }

        

    }

    public class LBSCustomPainterBox : LBSCustomPainter
    {
        protected override void DrawContent(MeshGenerationContext mgc)
        {
            var painter2D = mgc.painter2D;

            painter2D.DrawSelectionBox(MinPos, MaxPos, FillColor, strokeColor, lineWidth);
        }
    }

    public class LBSCustomPainterCircle : LBSCustomPainter
    {
        public float radius = 10f;

        protected override void DrawContent(MeshGenerationContext mgc)
        {
            var painter2D = mgc.painter2D;

            painter2D.DrawCircle(MinPos, radius, FillColor, strokeColor, lineWidth);
        }
    }
}
