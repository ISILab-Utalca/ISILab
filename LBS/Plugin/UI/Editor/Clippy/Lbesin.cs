using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

[UxmlElement]
public partial class Lbesin : VisualElement
{
    private readonly Vector2 offset = new Vector2(48,48);

    private Dictionary<VisualElement, List<EditorCoroutine>> _activeCoroutines = new ();
    private bool dragging;
    private Vector2 dragOffset;

    VisualElement _draggable;
    VisualElement _reset;
    VisualElement _modSelector;
    VisualElement[] _modButtons;

    VisualElement Draggable 
    {
        get => _draggable ??= this.Q<VisualElement>("Draggable");
    }
    VisualElement Reset
    {
        get => _reset ??= this.Q<VisualElement>("Reset");
    }
    VisualElement ModSelector
    {
        get => _modSelector ??= Draggable.Q<VisualElement>("ModSelector");
    }
    VisualElement[] ModButtons
    {
        get => _modButtons ??= new[]
        {
            ModSelector.Q<VisualElement>("1"),
            ModSelector.Q<VisualElement>("2"),
            ModSelector.Q<VisualElement>("3"),
            ModSelector.Q<VisualElement>("4")
        };
    }

    private Vector2 Position
    {
        get => new Vector2(Draggable.style.translate.value.x.value, Draggable.style.translate.value.y.value);
        set
        {
            Draggable.style.translate = new Translate(
                new Length(value.x, LengthUnit.Pixel), 
                new Length(value.y, LengthUnit.Pixel));
        }
    }

    public Lbesin() : base()
    {
        // Load the UXML file
        var visualTree = Resources.Load<VisualTreeAsset>("Lbesin");
        visualTree.CloneTree(this);

        // Set callbacks
        Draggable.RegisterCallback<PointerDownEvent>(OnPointerDown);
        Draggable.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        Draggable.RegisterCallback<PointerUpEvent>(OnPointerUp);
        Draggable.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

        Draggable.RegisterCallback<MouseEnterEvent>(evt => 
        {
            StartCoroutine(ShowImage(ModSelector), ModSelector);
            foreach (var button in ModButtons)
            {
                StartCoroutine(ShowImage(button), button);
            }
        });
        Draggable.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            StartCoroutine(HideImage(ModSelector), ModSelector);
            foreach (var button in ModButtons)
            {
                StartCoroutine(HideImage(button), button);
            }
        });

        Reset.RegisterCallback<ClickEvent>(evt => 
        {
            StartCoroutine(HideImage(Reset), Reset);
            ResetPosition();
        });

        // Initial values
        StartCoroutine(HideImage(Reset), Reset);
        StartCoroutine(HideImage(ModSelector), ModSelector);
        foreach (var button in ModButtons)
        {
            StartCoroutine(HideImage(button), button);
        }
    }

    #region COROUTINES
    private IEnumerator ShowImage(VisualElement ve)
    {
        var c = ve.style.unityBackgroundImageTintColor.value;
        var a = c.a;
        do
        {
            a += 0.2f;
            ve.style.unityBackgroundImageTintColor = new Color(c.r, c.g, c.b, a);
            yield return new EditorWaitForSeconds(0.05f);
        } while (a < 1);
        ve.style.unityBackgroundImageTintColor = new Color(c.r, c.g, c.b, 1);
    }
    private IEnumerator HideImage(VisualElement ve)
    {
        var c = ve.style.unityBackgroundImageTintColor.value;
        var a = c.a;
        do
        {
            a -= 0.2f;
            ve.style.unityBackgroundImageTintColor = new Color(c.r, c.g, c.b, a);
            yield return new EditorWaitForSeconds(0.05f);
        } while (a > 0);
        ve.style.unityBackgroundImageTintColor = new Color(c.r, c.g, c.b, 0);
    }
    #endregion

    #region POINTER EVENTS
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
    #endregion

    private void StartCoroutine(IEnumerator routine, VisualElement owner)
    {
        if(!_activeCoroutines.ContainsKey(owner))
        {
            _activeCoroutines[owner] = new List<EditorCoroutine>();
        }
        StopAllRoutines(owner);
        _activeCoroutines[owner].Add(EditorCoroutineUtility.StartCoroutine(routine, owner));
    }
    private void StopAllRoutines(VisualElement owner)
    {
        if (_activeCoroutines.ContainsKey(owner))
        {
            foreach (var coroutine in _activeCoroutines[owner])
            {
                EditorCoroutineUtility.StopCoroutine(coroutine);
            }
            _activeCoroutines[owner].Clear();
        }
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
