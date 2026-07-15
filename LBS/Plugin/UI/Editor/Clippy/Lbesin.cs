using ISILab.Commons.Interfaces;
using ISILab.Extensions;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.AI.Clippy.VisualElements
{
    [UxmlElement]
    public partial class Lbesin : VisualElement, IEasyEditorCoroutines
    {
        private readonly Color IconColor = new Color(0.306f, 0.937f, 0.737f, 1);
        private readonly Vector2 Offset = new Vector2(60, 48);
        private readonly Dictionary<Button, LbesinMod> Modes = new();

        private Dictionary<VisualElement, List<EditorCoroutine>> _activeCoroutines = new();
        private bool dragging;
        private bool isResetVisible;
        private Vector2 dragOffset;

        VisualElement _reset;
        VisualElement _draggable;
        VisualElement _icon;
        VisualElement _modSelector;
        VisualElement _modBackground;
        VisualElement _buttonsContainer;
        VisualElement _lbesinChatbox;
        Button[] _modButtons;

        VisualElement Reset
        {
            get => _reset ??= this.Q<VisualElement>("Reset");
        }
        VisualElement Draggable
        {
            get => _draggable ??= this.Q<VisualElement>("Draggable");
        }
        VisualElement Icon
        {
            get => _icon ??= this.Q<VisualElement>("Icon");
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
        VisualElement LbesinChatbox
        {
            get => _lbesinChatbox ??= this.Q<VisualElement>("LbesinChatbox");
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

        public Dictionary<VisualElement, List<EditorCoroutine>> ActiveCoroutines => _activeCoroutines;

        public Lbesin() : base()
        {
            // Load the UXML file
            var visualTree = Resources.Load<VisualTreeAsset>("Lbesin");
            visualTree.CloneTree(this);

            // Find Modes
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

            //--------------- CALLBACKS ---------------//

            // Draggable - Drag
            Draggable.RegisterCallback<PointerDownEvent>(OnPointerDown);
            Draggable.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            Draggable.RegisterCallback<PointerUpEvent>(OnPointerUp);
            Draggable.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            // Draggable - Fade
            Draggable.RegisterCallback<MouseEnterEvent>(evt => { this.ShowImage(ModSelector); });
            Draggable.RegisterCallback<MouseLeaveEvent>(evt => { this.HideImage(ModSelector); });

            // Reset
            Reset.RegisterCallback<ClickEvent>(evt => { 
                this.HideImage(Reset); 
                ResetPosition();
            });

            // Buttons
            foreach (var button in ModButtons)
            {
                var currentButton = button;
                currentButton.RegisterCallback<ClickEvent>(evt =>
                {
                    var source = (Button)evt.currentTarget;
                    var image = Modes[source].Icon;
                    var color = Modes[source].Color;

                    Icon.style.backgroundImage = new StyleBackground(image);
                    Icon.style.unityBackgroundImageTintColor = color;
                });
            }

            //--------------- INITIAL VALUES ---------------//
            Reset.style.unityBackgroundImageTintColor = IconColor;
            ModBackground.style.unityBackgroundImageTintColor = IconColor;

            isResetVisible = false;
            this.HideImage(Reset);
            this.HideImage(ModSelector);
        }

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
                this.ShowImage(Reset);
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
            this.HideImage(Reset);
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
}
