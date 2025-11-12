using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LBS.VisualElements
{
    [UxmlElement]
    public partial class NotifierViewer : VisualElement
    {
        private bool _notificationOn = true;
        private ScrollView _scrollView;
        private readonly VectorImage _iconNotificationsOn; 
        private readonly VectorImage _iconNotificationsOff;
        private const int FadeTime = 5;
        
        public NotifierViewer()
        {
            style.position = Position.Absolute;
            style.bottom = 0; 
            style.left = 0;   
            style.alignContent = Align.FlexStart; 
            style.justifyContent = Justify.FlexStart; 
            style.overflow = Overflow.Visible;
            
            _iconNotificationsOn = Resources.Load<VectorImage>("Icons/Vectorial/Icon=Notification");
            _iconNotificationsOff = Resources.Load<VectorImage>("Icons/Vectorial/Icon=MuteNotification");
            
           // SetContainer();
        }
        
        private void SetContainer()
        {
            if (_scrollView != null) return;
            
            _scrollView = this.Q<ScrollView>("MessageContainer");
  
            // disable as a clickable 
            pickingMode = PickingMode.Ignore;
            _scrollView.pickingMode = PickingMode.Ignore;
            VisualElement container = _scrollView.Q<VisualElement>("unity-content-and-vertical-scroll-container");
            container.pickingMode = PickingMode.Ignore;
            
            _scrollView.contentViewport.style.justifyContent = Justify.FlexStart;

            // Make sure the content does not grow; list with fixed height size
            _scrollView.contentViewport.style.flexGrow = 1f;

            _scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            _scrollView.contentViewport.style.flexDirection = FlexDirection.Row;
            _scrollView.contentContainer.style.flexDirection = FlexDirection.Column;
            
        }
        
        /*
        private NotificationMessage[] GetChildren()
        {
            List<NotificationMessage> messages = new List<NotificationMessage>();
            var veChildren = _scrollView.Children().ToArray();
            foreach (VisualElement veChild in veChildren)
            {
                if(veChild is NotificationMessage message) messages.Add(message);
            }
            
            return messages.ToArray();
        }
        */
        
        public void SendNotification(string message, LogType logType, int duration)
        {
            SetContainer();
            NotificationMessage newMessage = new();
            
            newMessage.SetData(message, logType);
            newMessage.pickingMode = PickingMode.Ignore;
            _scrollView.Add(newMessage);
            
            Lifetime(newMessage, duration);
        }
        
        private async void Lifetime(NotificationMessage element, float duration)
        {
            // Ensure the duration is valid
            if (duration > 0)
            {
                Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew(); 
                while (stopwatch.Elapsed.TotalSeconds < duration)
                {
                    await Task.Yield();
                }
                stopwatch.Stop();
            }
            FadeOut(element);
        }

        private async void FadeOut(NotificationMessage element)
        {
            // Ensure the duration is valid
            if (FadeTime > 0)
            {
                float startOpacity = element.resolvedStyle.opacity;
                float elapsed = 0f;

                while (elapsed < FadeTime)
                {
                    await Task.Yield(); 
                    elapsed += Time.deltaTime;
                    float t = elapsed / FadeTime; 
                    element.style.opacity = Mathf.Lerp(startOpacity, 0f, t);
                }
            }
            
            element.style.opacity = 0f;
            if(_scrollView.childCount >0 && _scrollView.Contains(element)) _scrollView.Remove(element);
        }

        public void ClearNotifications()
        {
            _scrollView?.Clear();
        }
        
        public void NotificationFlipFlop(VisualElement button)
        {
            _notificationOn = !_notificationOn;
            if (button == null) return;
            ToolbarButton tButton = button as ToolbarButton;
            if (tButton == null) return;
            VisualElement ve = button.Children().First();
            
            ve.style.backgroundImage = _notificationOn ? new StyleBackground(_iconNotificationsOn) : new StyleBackground(_iconNotificationsOff);
            
        }
        
        public void SetButtons(VisualElement cleanButton, VisualElement disableNotificationButton)
        {
            cleanButton.RegisterCallback<ClickEvent>(_ => ClearNotifications());
            disableNotificationButton.RegisterCallback<ClickEvent>(vt => NotificationFlipFlop(disableNotificationButton));
        }

    }
}
  

