using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class Lbesin : VisualElement
{
    private readonly Vector2 offset = new Vector2(48,48);

    private bool dragging;
    private Vector2 dragOffset;

    VisualElement _draggable;
    VisualElement _reset;

    VisualElement Draggable 
    {
        get => _draggable ??= this.Q<VisualElement>("Draggable");
    }
    VisualElement Reset
    {
        get => _reset ??= this.Q<VisualElement>("Reset");
    }

    private Vector2 Position
    {
        get => new Vector2(Draggable.style.translate.value.x.value, Draggable.style.translate.value.y.value);
        set
        {
            Draggable.style.translate = new Translate(new Length(value.x, LengthUnit.Pixel), new Length(value.y, LengthUnit.Pixel));
        }
    }

    public Lbesin() : base()
    {
        // Load the UXML file
        var visualTree = Resources.Load<VisualTreeAsset>("Lbesin");
        visualTree.CloneTree(this);

        Draggable.RegisterCallback<PointerDownEvent>(OnPointerDown);
        Draggable.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        Draggable.RegisterCallback<PointerUpEvent>(OnPointerUp);
        Draggable.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

        Reset.RegisterCallback<ClickEvent>(evt => ResetPosition());
    }


    private void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0) // Left mouse button
            return;

        dragging = true;
        dragOffset = evt.localPosition;
        Debug.Log("Offset: " + dragOffset);

        PointerCaptureHelper.CapturePointer(Draggable, evt.pointerId);
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!dragging || !PointerCaptureHelper.HasPointerCapture(Draggable, evt.pointerId))
            return;

        Position = new Vector2(evt.position.x - dragOffset.x - offset.x, evt.position.y - dragOffset.y - offset.y);
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!PointerCaptureHelper.HasPointerCapture(Draggable, evt.pointerId))
            return;

        dragging = false;
        PointerCaptureHelper.ReleasePointer(Draggable, evt.pointerId);
        KeepOnBounds();
    }

    private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        dragging = false;
    }

    private void KeepOnBounds()
    {
        if(Position.x < 0 || Position.y < 0)
        {
            Position = new Vector2(Mathf.Max(Position.x, 0), Mathf.Max(Position.y, 0));
        }
    }

    private void ResetPosition()
    {
        Position = Vector2.zero;
    }
}
