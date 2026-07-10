using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class Lbesin : VisualElement
{
    private readonly Color SelectorColor = new Color(0.125f, 0.216f, 0.851f, 1);
    private readonly Color IconColor = new Color(0.306f, 0.937f, 0.737f, 1);
    private readonly Vector2 offset = new Vector2(60,48);
    private readonly Dictionary<Button, string> _icons = new();

    private Dictionary<VisualElement, List<EditorCoroutine>> _activeCoroutines = new ();
    private bool dragging;
    private bool isResetVisible;
    private Vector2 dragOffset;

    VisualElement _draggable;
    VisualElement _icon;
    VisualElement _reset;
    VisualElement _modSelector;
    VisualElement _modBackground;
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
    Button[] ModButtons
    {
        get => _modButtons ??= new[]
        {
            this.Q<Button>("1"),
            this.Q<Button>("2"),
            this.Q<Button>("3"),
            this.Q<Button>("4")
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

        _icons[ModButtons[0]] = "Icons/Vectorial/Population/Icon=Scroll";
        _icons[ModButtons[1]] = "Icons/Vectorial/Population/Icon=Hearth";
        _icons[ModButtons[2]] = "Icons/Vectorial/Population/Icon=Helmet";
        _icons[ModButtons[3]] = "Icons/Vectorial/SideToolBar/Icon=AI_Assistant";

        // Set callbacks
        Draggable.RegisterCallback<PointerDownEvent>(OnPointerDown);
        Draggable.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        Draggable.RegisterCallback<PointerUpEvent>(OnPointerUp);
        Draggable.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

        Draggable.RegisterCallback<MouseEnterEvent>(evt => 
        {
            ShowImage(ModBackground);
            foreach (var button in ModButtons)
            {
                ShowImage(button);
            }
        });
        Draggable.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            HideImage(ModBackground);
            foreach (var button in ModButtons)
            {
                HideImage(button);
            }
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
                var source = (Button)evt.currentTarget;

                var path = _icons[(Button)evt.currentTarget];

                var image = Resources.Load<VectorImage>(path);

                Icon.style.backgroundImage = new StyleBackground(image);
            });
        }

        // Initial values
        Reset.style.unityBackgroundImageTintColor = IconColor;
        ModBackground.style.unityBackgroundImageTintColor = SelectorColor;
        foreach (var button in ModButtons)
        {
            var currentButton = button;
            currentButton.style.unityBackgroundImageTintColor = IconColor;
        }

        isResetVisible = false;
        HideImage(Reset);
        HideImage(ModBackground);
        foreach (var button in ModButtons)
        {
            var currentButton = button;
            HideImage(  currentButton);
        }
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
                4 * deltaTime);

            ve.style.unityBackgroundImageTintColor = color;

            yield return null;
        }

        color.a = targetAlpha;
        ve.style.unityBackgroundImageTintColor = color;
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

    private void KeepOnBounds()
    {
        if(Position.x < 0 || Position.y < 0)
        {
            Position = new Vector2(Mathf.Max(Position.x, 0), Mathf.Max(Position.y, 0));
        }
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
}
