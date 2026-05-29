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
        
        private Color _bgColor = Color.white;
        private float angle_deg = 45;
        private float _height = 30;
        
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


        public LBSPainterVisualElement(): base()
        {
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
                Color.white
                );
        }

        void OnGeometryChanged(GeometryChangedEvent _evt)
        { 
            _visElementRect = contentRect;
        }
    }
}
