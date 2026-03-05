using LBS.Components;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public abstract partial class QuickAssistantContainer : VisualElement
    {
        public abstract string PrimaryKeyword{ get; }
        public abstract string SecondaryKeyword { get; }
        public abstract void LoadVisualElements();

        public abstract void InitialSetup();

        public abstract Task GenerateLayerProcess(LBSLayer newLayer);
    }
}
