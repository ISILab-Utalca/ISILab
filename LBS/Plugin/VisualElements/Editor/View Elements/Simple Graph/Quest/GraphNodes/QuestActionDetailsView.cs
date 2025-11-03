using System;
using ISILab.Commons.Utility.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.Macros;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class QuestActionDetailsView : VisualElement
    {
        private static VisualTreeAsset _asset;

        private const string ConnectionIconGuid = "ec280cec81783e94cb5df0b0b40dec7e";
        private const string GrammarIconGuid = "7bdf2adeb17673349abf65c6f8f0f411";
        private const string DataIconGuid = "5549d02f87d9642469d0336544f4cb88";

        private const string ConnectionDescription = "Indicates that the Node must be connected to another action node. Or the connection is invalid.";
        private const string GrammarDescription = "Indicates that the position of the node in the graph is not gramatically valid. To see what actions can make it valid, check the Grammar Assistant.";
        private const string DataDescription = "Indicates that the node's data is not complete, see the Node Behaviour Panel and make sure all references are set and complete.";

        private const string PanelDescription =
            "If any of the following icons are displayed in the node the graph won't be generated (using 3D Generator) in the scene.";

        public QuestActionDetailsView()
        {
            if (_asset == null)
                _asset = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestActionDetailsView");

            _asset.CloneTree(this);
            
            VisualElement connectionSection = CreateInfoSection("Connection", ConnectionDescription, "Use Connection Tool", ConnectionIconGuid, OnConnectionClicked);
            VisualElement grammarSection = CreateInfoSection("Grammar",GrammarDescription , "See Grammatically Valid Options.", GrammarIconGuid, OnGrammarClicked);
            VisualElement dataSection = CreateInfoSection("Data",DataDescription , "Go to Node Data Information", DataIconGuid, OnDataClicked);

            Label descriptionLabel = new(PanelDescription)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Italic,
                    fontSize = 11,
                    color = new StyleColor(new Color(0.75f, 0.75f, 0.75f)),
                    marginBottom = 6,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            
            Add(descriptionLabel);
            Add(connectionSection);
            Add(grammarSection);
            Add(dataSection);

            style.flexDirection = FlexDirection.Column;
            style.paddingTop = 4;
            style.paddingBottom = 4;
            style.paddingLeft = 6;
            style.paddingRight = 6;
        }
        
        #region Button Callbacks
        private void OnConnectionClicked()
        {
            Debug.Log("Connection Clicked");
        }

        private void OnGrammarClicked()
        {
            Debug.Log("Grammar Clicked");
        }

        private void OnDataClicked()
        {
            Debug.Log("Data Clicked");
        }
        #endregion

     private VisualElement CreateInfoSection(string title, string description, string buttonLabel, string iconGuid, Action onClick)
    {
        VisualElement container = new()
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                justifyContent = Justify.SpaceBetween,
                marginBottom = 4,
                paddingTop = 4,
                paddingBottom = 4,
                paddingLeft = 4,
                paddingRight = 4,
                borderBottomColor = new Color(0.25f, 0.25f, 0.25f),
                borderBottomWidth = 1,
                borderBottomLeftRadius = 2,
                borderBottomRightRadius = 2
            }
        };

        // Icon (fixed size)
        VisualElement icon = new()
        {
            name = $"{title}Icon",
            style =
            {
                width = 24,
                height = 24,
                backgroundImage = new StyleBackground(LBSAssetMacro.LoadAssetByGuid<VectorImage>(iconGuid)),
                marginRight = 8,
                flexShrink = 0
            }
        };

        // Text group (fills half width)
        VisualElement textGroup = new()
        {
            style =
            {
                flexDirection = FlexDirection.Column,
                flexGrow = 1,
                flexBasis = Length.Percent(50),
                marginRight = 6
            }
        };

        Label nameLabel = new(title)
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 12
            }
        };

        Label descLabel = new(description)
        {
            style =
            {
                fontSize = 10,
                color = new StyleColor(new Color(0.8f, 0.8f, 0.8f)),
                whiteSpace = WhiteSpace.Normal
            }
        };

        textGroup.Add(nameLabel);
        textGroup.Add(descLabel);

        // Button (fills the other half)
        Button button = new(() => onClick?.Invoke())
        {
            text = buttonLabel,
            style =
            {
                flexGrow = 1,
                flexBasis = Length.Percent(50),
                height = StyleKeyword.Auto,
                fontSize = 10,
                unityTextAlign = TextAnchor.MiddleCenter,
                marginLeft = 6,
                marginRight = 4,
                paddingLeft = 8,
                paddingRight = 8
            }
        };

        // Put it all together
        container.Add(icon);
        container.Add(textGroup);
        container.Add(button);

        return container;
    }

    }
}
