using ISILab.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class Lbesin : VisualElement
{
    private readonly Color IconColor = new Color(0.306f, 0.937f, 0.737f, 1);
    private readonly Vector2 Offset = new Vector2(60,48);
    private readonly Dictionary<Button, LbesinMod> Modes = new();

    private Dictionary<VisualElement, List<EditorCoroutine>> _activeCoroutines = new ();
    private bool dragging;
    private bool isResetVisible;
    private Vector2 dragOffset;

    VisualElement _draggable;
    VisualElement _icon;
    VisualElement _reset;
    VisualElement _modSelector;
    VisualElement _modBackground;
    VisualElement _buttonsContainer;
    Button[] _modButtons;


    VisualElement Draggable 
    {
        get => _draggable ??= this.Q<VisualElement>("Draggable");
    }
    VisualElement Icon
    {
        get => _icon ??= this.Q<VisualElement>("Icon");
    }
    VisualElement Reset
    {
        get => _reset ??= this.Q<VisualElement>("Reset");
    }
    VisualElement ModSelector
    {
        get => _modSelector ??= Draggable.Q<VisualElement>("ModSelector");
    }
    VisualElement ModBackground
    {
        get => _modBackground ??= this.Q<VisualElement>("ModBackground");
    }
    VisualElement ButtonsContainer
    {
        get => _buttonsContainer ??= this.Q<VisualElement>("ButtonsContainer");
    }
    Button[] ModButtons
    {
        get => _modButtons;
        set => _modButtons = _modButtons is null ? value : throw new System.InvalidOperationException("modButtons can only be set once.");
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

        // Create Mods
        var modes = Resources.FindObjectsOfTypeAll<LbesinMod>().OrderBy(m => m.SortingIndex).ToArray();
        var buttons = new List<Button>();
        for (int i = 0; i < modes.Length; i++)
        {
            var m = modes[i];
            var b = m.GenerateButton();
            Modes[b] = m;

            SetButtonPosition(b, 45 * (i - (modes.Length / 2)));
            ButtonsContainer.Add(b);
            buttons.Add(b);
        }
        ModButtons = buttons.ToArray();

        // Set callbacks
        Draggable.RegisterCallback<PointerDownEvent>(OnPointerDown);
        Draggable.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        Draggable.RegisterCallback<PointerUpEvent>(OnPointerUp);
        Draggable.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

        Draggable.RegisterCallback<MouseEnterEvent>(evt => 
        {
            ShowImage(ModSelector);
        });
        Draggable.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            HideImage(ModSelector);
        });

        Reset.RegisterCallback<ClickEvent>(evt => 
        {
            HideImage(Reset);
            ResetPosition();
        });

        foreach (var button in ModButtons)
        {
            var currentButton = button;
            currentButton.RegisterCallback<ClickEvent>(evt =>
            {
                var source = (Button) evt.currentTarget;
                var image = Modes[source].Icon;
                var color = Modes[source].Color;

                Icon.style.backgroundImage = new StyleBackground(image);
                Icon.style.unityBackgroundImageTintColor = color;
            });
        }

        // Initial values
        Reset.style.unityBackgroundImageTintColor = IconColor;
        ModBackground.style.unityBackgroundImageTintColor = IconColor;

        isResetVisible = false;
        HideImage(Reset);
        HideImage(ModSelector);
    }

    #region COROUTINES
    private void StartCoroutine(IEnumerator routine, VisualElement owner)
    {
        if (!_activeCoroutines.ContainsKey(owner))
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
    private IEnumerator FadeImage(VisualElement ve, float targetAlpha)
    {
        Color color = ve.style.unityBackgroundImageTintColor.value;

        double previousTime = UnityEditor.EditorApplication.timeSinceStartup;

        while (!Mathf.Approximately(color.a, targetAlpha))
        {
            double currentTime = UnityEditor.EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - previousTime);
            previousTime = currentTime;

            color.a = Mathf.MoveTowards(
                color.a,
                targetAlpha,
                8 * deltaTime);

            ve.style.unityBackgroundImageTintColor = color;

            yield return null;
        }

        color.a = targetAlpha;
        ve.style.unityBackgroundImageTintColor = color;

        foreach(VisualElement son in ve.Children())
        {
            StartCoroutine(FadeImage(son, targetAlpha), son);
        }
    }
    private void ShowImage(VisualElement ve) => StartCoroutine(FadeImage(ve, 1f), ve);
    private void HideImage(VisualElement ve) => StartCoroutine(FadeImage(ve, 0f), ve);
    #endregion

    #region POINTER EVENTS
    private void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0) // Left mouse button
            return;
        if (evt.target is Button)
            return;

        dragging = true;
        dragOffset = evt.localPosition;
        Debug.Log("Offset: " + dragOffset);

        PointerCaptureHelper.CapturePointer(Draggable, evt.pointerId);
        //evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!dragging || !PointerCaptureHelper.HasPointerCapture(Draggable, evt.pointerId))
            return;

        if (!isResetVisible)
        {
            isResetVisible = true;
            ShowImage(Reset);
        }

        Position = new Vector2(evt.position.x - dragOffset.x - Offset.x, evt.position.y - dragOffset.y - Offset.y);
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

    private void KeepOnBounds()
    {
        if (parent == null) return;

        Rect parentRect = parent.layout;
        Rect myRect = Draggable.layout;

        Vector2 pos = Position;
        pos.x = Mathf.Clamp(pos.x, 0 - Offset.x, parentRect.width - myRect.width - Offset.x);
        pos.y = Mathf.Clamp(pos.y, 0, parentRect.height - myRect.height);
        Position = pos;
    }
    private void ResetPosition()
    {
        isResetVisible = false;
        HideImage(Reset);
        Position = Vector2.zero;
    }

    public void SetDisplay(DisplayStyle ds)
    {
        this.style.display = ds;
    }

    private readonly Vector2 ButtonRadius = Vector2.right * 45;
    private void SetButtonPosition(Button button, float angle)
    {
        var pos = ButtonRadius.Rotate(angle);

        button.style.translate = new Translate(
            new Length(pos.x, LengthUnit.Pixel),
            new Length(pos.y, LengthUnit.Pixel));
    }
}
