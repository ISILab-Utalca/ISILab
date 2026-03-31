using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using LBS.VisualElements;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class QuestActionDetailsView : VisualElement
    {
        private static VisualTreeAsset _asset;
        private readonly LBSInteractiveTooltip connectionItt;
        private readonly LBSInteractiveTooltip grammarItt;
        private readonly LBSInteractiveTooltip dataItt;
        
        // Node assigned to the QuestActionView that uses this panel
        private readonly QuestNode node;
        
        public QuestNode Node { get; set; }
        
        public QuestActionDetailsView()
        {
            if (_asset == null)
                _asset = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestActionDetailsView");

            _asset.CloneTree(this);
            
            connectionItt = this.Q<LBSInteractiveTooltip>("Connection");
            grammarItt =  this.Q<LBSInteractiveTooltip>("Grammar");
            dataItt = this.Q<LBSInteractiveTooltip>("Data");
            
            connectionItt.SetAction(OnConnectionClicked);
            grammarItt.SetAction(OnGrammarClicked);
            dataItt.SetAction(OnDataClicked);
        }
        
        #region Button Callbacks
        private void OnConnectionClicked()
        {
            ToolKit.Instance.SetActive(typeof(ConnectQuestNodes));
            if(ToolKit.Instance.GetToolButton(typeof(ConnectQuestNodes)) is ToolButton tb)
            {
                LBSFocusHighlight.Highlight(tb);
            }
        }

        private void OnGrammarClicked()
        {
            var bh = Node?.Graph?.OwnerLayer?.GetBehaviour<QuestBehaviour>();
            if (bh != null) bh.SelectedGraphNode = Node;
            LBSInspectorPanel.ActivateAssistantTab();
            VisualElement grammarAssistant = LBSInspectorPanel.Instance.ActiveInspector.GetInspector(
                typeof(GrammarAssistant), LBSMainWindow.Instance._selectedLayer);
            
            LBSFocusHighlight.Highlight(grammarAssistant);
        }
    
        private void OnDataClicked()
        {
            var bh = Node?.Graph?.OwnerLayer?.GetBehaviour<QuestBehaviour>();
            if (bh != null) bh.SelectedGraphNode = Node;
            LBSInspectorPanel.ActivateBehaviourTab();
            VisualElement nodeBehaviour = LBSInspectorPanel.Instance.ActiveInspector.GetInspector(
                typeof(QuestNodeBehaviour), LBSMainWindow.Instance._selectedLayer);
            
            LBSFocusHighlight.Highlight(nodeBehaviour);
        }

        #endregion
        
        /// <summary>
        /// Pass the icons of each of the possible generation problems.
        ///
        /// Display the tooltips based on the existing issues.
        /// </summary>
        public void SetDisplays(VisualElement connection, VisualElement grammar, VisualElement data)
        {
            connectionItt.style.display = connection.style.display;
            grammarItt.style.display = grammar.style.display;
            dataItt.style.display = data.style.display;
            
            if(connectionItt.style.display == DisplayStyle.Flex 
               || grammarItt.style.display == DisplayStyle.Flex 
               || dataItt.style.display == DisplayStyle.Flex
               )
            {
                style.display = DisplayStyle.Flex;
            }
            else
            {
                style.display = DisplayStyle.None;
            }
        }
    }
}
