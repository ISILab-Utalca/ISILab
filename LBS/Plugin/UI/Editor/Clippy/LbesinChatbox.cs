using ISILab.Commons.Interfaces;
using ISILab.LBS.CustomComponents;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.AI.Clippy.VisualElements
{
    [UxmlElement]
    public partial class LbesinChatbox : VisualElement, IEasyEditorCoroutines
    {
        private Dictionary<VisualElement, List<EditorCoroutine>> _activeCoroutines = new ();

        private readonly Vector2 MinSize = new Vector2(150, 40);

        private VisualElement _chatbox;
        private VisualElement _background;
        private LBSCustomLabel _label;
        private ListView _suggestionList;
        private VisualElement _dragEdge;
        private Button _closeButton;

        private VisualElement Chatbox
        {
            get => _chatbox ??= this.Q<VisualElement>("Chatbox");
        }
        private VisualElement Background
        {
            get => _background ??= this.Q<VisualElement>("Background");
        }
        private LBSCustomLabel Label
        {
            get => _label ??= this.Q<LBSCustomLabel>("Label");
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

        public Dictionary<VisualElement, List<EditorCoroutine>> ActiveCoroutines => _activeCoroutines;

        public Color Tint
        {
            set
            {
                Background.style.borderRightColor = value;
                Background.style.borderBottomColor = value;
                Background.style.borderLeftColor = value;
                Background.style.borderTopColor = value;
                Label.style.color = value;
            }
        }
        private bool Display
        {
            set => this.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            get => this.style.display == DisplayStyle.Flex;
        }


        public LbesinChatbox() : base()
        {
            // Load the UXML file
            var visualTree = Resources.Load<VisualTreeAsset>("LbesinChatbox");
            visualTree.CloneTree(this);

            CloseButton.clicked += () => { this.StartCoroutine(CloseChatbox(), this); };
            //this.StartCoroutine(CloseChatbox(), this);
        }


        private IEnumerator CloseChatbox()
        {
            Debug.Log(Display);
            if (!Display) yield break;
            yield return this.FadeOpacity(Chatbox, 0);
            Display = false;
        }
        public IEnumerator OpenChatbox()
        {
            Debug.Log(Display);
            if (Display) yield break;
            Display = true;
            yield return this.FadeOpacity(Chatbox, 1);
        }
    }
}
