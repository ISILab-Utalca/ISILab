using ISILab.Extensions;
using UnityEngine;
using Unity.UIElements;
using UnityEngine.UIElements;


namespace ISILab.LBS.CustomComponents
{

    [UxmlElement]
    public partial class LBSPainterVisualElement: VisualElement
    {
        
        private Rect _visElementRect = Rect.zero;

        protected Color strokeColor;
        protected int lineWidth;

        private Color _bgColor = Color.white;
        private float angle_deg = 45;
        private float _height = 30;
        private bool isVertical = false;

        [UxmlAttribute]
        public bool IsVertical
        {
            get => isVertical;
            set
            {
                isVertical = value;
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute] 
        public Color BGcolor
        {
            get => _bgColor;
            set
            {
                _bgColor = value;
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public float AngleDeg
        {
            set
            {
                value = Mathf.Clamp(value, 0f, 90f);
                angle_deg = value;
                MarkDirtyRepaint();
            }
            get => angle_deg;
        }
        
        [UxmlAttribute]
        public float Height
        {
            set
            {
                _height = value;
                MarkDirtyRepaint();
            }
            get => _height;
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

        public LBSPainterVisualElement(): base()
        {
            strokeColor = Color.black;
            lineWidth = 1;

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
 
        }

        public void OnGenerateVisualContent (MeshGenerationContext _ctx)
        {
            if (_visElementRect == Rect.zero) return;
            Rect r = contentRect;
    

            Painter2D painter = _ctx.painter2D;
            painter.DrawSquare(r.position, r.size.y, r.size.x, _bgColor);
            painter.DrawTrapezoid(
                r.position,
                r.size.x,
                _height,
                angle_deg,
                _bgColor,
                isVertical,
                strokeColor,
                lineWidth
                );

            //this.SetBorder(Color.black, 0);
        }

        void OnGeometryChanged(GeometryChangedEvent _evt)
        { 
            _visElementRect = contentRect;
 
        }
    }
}
