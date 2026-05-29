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
        
        [UxmlAttribute] 
        public Color BGcolor { get => _bgColor; set => _bgColor = value; }
        
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
        }

        void OnGeometryChanged(GeometryChangedEvent _evt)
        { 
            _visElementRect = contentRect;
        }
    }
}
