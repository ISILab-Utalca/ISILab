using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.AI.Clippy.VisualElements
{
    [UxmlElement]
    public partial class LbesinChatbox : VisualElement
    {
        private readonly Vector2 MinSize = new Vector2(150, 40);

        private VisualElement _chatbox;
        private ListView _suggestionList;
        private VisualElement _dragEdge;
        private Button _closeButton;

        private VisualElement Chatbox
        {
            get => _chatbox ??= this.Q<VisualElement>("Chatbox");
        }
        private ListView SuggestionList
        {
            get => _suggestionList ??= this.Q<ListView>("SuggestionList");
        }
        private VisualElement DragEdge
        {
            get => _dragEdge ??= Chatbox.Q<VisualElement>("DragEdge");
        }
        private Button CloseButton
        {
            get => _closeButton ??= this.Q<Button>("CloseButton");
        }

        public bool Display
        {
            set => this.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            get => this.style.display == DisplayStyle.Flex;
        }

        public LbesinChatbox() : base()
        {
            // Load the UXML file
            var visualTree = Resources.Load<VisualTreeAsset>("LbesinChatbox");
            visualTree.CloneTree(this);

            //DragEdge.RegisterCallback<>
        }
    }
}
