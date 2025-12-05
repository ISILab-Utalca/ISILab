using ISILab.LBS.Plugin.Core.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    /// <summary>
    /// Meant to be used to highlight any VisualElement so the user knows where to find
    /// information. To use:
    ///
    /// <code>
    /// VisualElement ve = new VisualElement();
    /// LBSFocusHighlight.Highlight(ve);
    ///</code>
    /// 
    /// </summary>
    public class LBSFocusHighlight : VisualElement
    {
        private VisualElement _border;
        private VisualElement _background;

        private const float BorderWidth = 2f;
        private const float BorderOpacity = 1f;
        private const float BackgroundOpacity = 0.5f;

        private IVisualElementScheduledItem _animationHandle;
        private float _time;
        private float _elapsed;
        private const long _animationFramerate = 16;

        private System.Diagnostics.Stopwatch _timer;

        private static float Duration { get; set; } = 1.4f;
        private float AnimationSpeed { get; set; } = 0.01f;

        private static Color _borderColor = LBSSettings.Instance.view.newToolkitSelected;
        private static Color _backgroundColor = new(_borderColor.r, _borderColor.g, _borderColor.b, BackgroundOpacity);

        private VisualElement CreateVisualElement()
        {
            pickingMode = PickingMode.Ignore;

            // Background tint overlay
            _background = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    top = 0,
                    bottom = 0,
                    left = 0,
                    right = 0,
                    backgroundColor = _backgroundColor,
                    opacity = 0
                }
            };

            // Border overlay
            _border = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    top = -BorderWidth / 2,
                    bottom = -BorderWidth / 2,
                    left = -BorderWidth / 2,
                    right = -BorderWidth / 2,
                    borderTopWidth = BorderWidth,
                    borderBottomWidth = BorderWidth,
                    borderLeftWidth = BorderWidth,
                    borderRightWidth = BorderWidth,
                    borderTopColor = _borderColor,
                    borderBottomColor = _borderColor,
                    borderLeftColor = _borderColor,
                    borderRightColor = _borderColor,
                    opacity = 0
                }
            };

            Add(_background);
            Add(_border);

            style.position = Position.Absolute;
            PlayAnimation();

            return this;
        }
        public LBSFocusHighlight()
        {
            CreateVisualElement();
        }

        public LBSFocusHighlight(float duration, Color borderColor = default, Color backgroundColor = default)
        {
            Duration = duration;
            _borderColor = borderColor;
            _backgroundColor = backgroundColor;
            CreateVisualElement();
        }

        /// <summary>
        /// Plays the highlight animation.
        /// </summary>
        private void PlayAnimation()
        {
            _animationHandle?.Pause();

            _timer = System.Diagnostics.Stopwatch.StartNew();
            _time = 0;
            _animationHandle = schedule.Execute(UpdateAnimation).Every(_animationFramerate);
        }

        private void UpdateAnimation()
        {
            _time += _animationFramerate * AnimationSpeed; 
            float pulse = Mathf.Abs(Mathf.Sin(_time));

            _border.style.opacity = pulse * BorderOpacity;
            _background.style.opacity = pulse * BackgroundOpacity;

            if (_timer.Elapsed.TotalSeconds >= Duration)
                StopAnimation();
        }

        private void StopAnimation()
        {
            _animationHandle?.Pause();
            _border.style.opacity = 0;
            _background.style.opacity = 0;

            parent?.Remove(this);
        }
        
        public static void Highlight(VisualElement target, float duration = -1, Color borderColor = default, Color backgroundColor = default)
        {
            if (target?.parent == null) return;
            
            if (duration < 0) duration = Duration;
            if (borderColor == default) borderColor = _borderColor;
            if (backgroundColor == default) backgroundColor = _backgroundColor;
                

            LBSFocusHighlight highlight = new LBSFocusHighlight(duration, borderColor, backgroundColor);

            // this visual element is added in front using its parent as reference
            target.parent.Add(highlight);
            highlight.PlaceInFront(target);
            highlight.style.position = Position.Absolute;

            // change update on geometry change in case the parent or target have not resolved their layout yet
            target.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                Rect world = target.worldBound;
                ChangeLayout(target, world, highlight);
            });

            // Immediate trigger if layout is resolved
            Rect worldNow = target.worldBound;
            if (!(worldNow.width > 0) || !(worldNow.height > 0)) return;
            {
                ChangeLayout(target, worldNow, highlight);
            }
        }

        private static void ChangeLayout(VisualElement target, Rect worldNow, LBSFocusHighlight highlight)
        {
            Rect local = target.parent.WorldToLocal(worldNow);
            highlight.style.left = local.xMin;
            highlight.style.top = local.yMin;
            highlight.style.width = local.width;
            highlight.style.height = local.height;
        }
    }
}
