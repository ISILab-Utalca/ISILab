using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public abstract partial class QuickAssistantContainer : VisualElement
    {
        public abstract void LoadVisualElements();

    }
}
