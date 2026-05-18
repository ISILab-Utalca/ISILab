using UnityEngine.UIElements;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Core.AI.Assistant;

namespace ISILab.LBS.VisualElements.Editor
{
    [UxmlElement]
    public partial class QuestNodeSuggestion : VisualElement
    {
        #region UXMLFACTORY
        [UxmlElementAttribute]
        public new class UxmlFactory { }
        #endregion

        #region ATTRIBUTES
        private readonly Label _actionLabel;
        private Button _goToButton;
        private Button _applyButton;
        private Button _discardButton;
        #endregion

        #region FIELDS

        private QuestNode _generatedQuestNode;
        
        #endregion
        
   
        public QuestNodeSuggestion() {
            
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestNodeSuggestion");
            visualTree.CloneTree(this);
            
            _actionLabel = this.Q<Label>("ActionLabel");
            _goToButton = this.Q<Button>("GoToButton");
            _applyButton = this.Q<Button>("ApplyButton");
            _discardButton = this.Q<Button>("DiscardButton");
            
            _discardButton.clicked += RemoveSuggestion;
            _applyButton.clicked += AddSuggestionToGraph;
            _goToButton.clicked += GoToNode;

        }

        private void RemoveSuggestion() => _generatedQuestNode.Graph.OwnerLayer.GetAssistant<QuestAssistant>().OnSuggestionRemoved?.Invoke(_generatedQuestNode);

        private void AddSuggestionToGraph()
        {
            _generatedQuestNode.Graph.AddSuggestionNode(_generatedQuestNode);
            RemoveSuggestion();
        }

        private void GoToNode()
        {
            var graphPos = _generatedQuestNode.Graph.OwnerLayer.FixedToPosition(_generatedQuestNode.NodePosition.position.ToInt(), true);
            _generatedQuestNode.Graph.GoToNodeInGraph(graphPos.ToInt());
        }

        public void UpdateData(QuestNode genNode)
        {
            if (genNode == null) return;
            _generatedQuestNode = genNode;
            _actionLabel.text = genNode.TerminalID;
        }
    }
}