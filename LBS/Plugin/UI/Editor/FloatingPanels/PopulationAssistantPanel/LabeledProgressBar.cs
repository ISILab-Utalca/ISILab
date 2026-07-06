using ISILab.Commons.Utility.Editor;
using ISILab.LBS.AI.Categorization;
using ISILab.LBS.CustomComponents;
using System;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;
// using Palmmedia.ReportGenerator.Core.Parser.Analysis;

namespace ISILab.LBS.VisualElements.Editor
{
    [UxmlElement]
    public partial class LabeledProgressBar : VisualElement
    {
        #region ATTRIBUTES

        [UxmlAttribute]
        public Color ProgressThemeColor
        {
            get => bar.ProgressThemeColor;
            set
            {
                bar.ProgressThemeColor = value;
            }
        }

        [UxmlAttribute]
        public Color IconThemeColor
        {
            get => bar.IconThemeColor;
            set
            {
                bar.IconThemeColor = value;
            }
        }


        [UxmlAttribute]
        public VectorImage ProgressIconImage
        {
            get => bar.ProgressIconImage;
            set
            {
                bar.ProgressIconImage = value;
            }
        }

        [UxmlAttribute]
        public string ProgressTextLabel
        {
            get => bar.ProgressTextLabel;
            set
            {
                bar.ProgressTextLabel = value;

            }
        }

        [UxmlAttribute]
        public string TitleText
        {
            get => label.text;
            set
            {
                label.text = value;
            }
        }
        #endregion

        # region FIELDS
        private string text = string.Empty; 
        private LBSCustomProgressBar bar;
        private LBSCustomLabel label;
        private static VisualTreeAsset visualTree;
        #endregion

        #region EVENTS
        // public Action OnExecute;
        #endregion

        #region PROPERTIES

        public LBSCustomProgressBar Bar
        {
            get => bar;
            set => bar = value;
        }

        public LBSCustomLabel Label
        {
            get => label;
            set => label = value;
        }

        #endregion

        #region CONSTRUCTORS
        public LabeledProgressBar()
        {
            visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("LabeledProgressBar");
            visualTree.CloneTree(this);

            label = this.Q<LBSCustomLabel>();

            label.RegisterValueChangedCallback(evt => 
            {
                text = evt.newValue;
            });

            bar = this.Q<LBSCustomProgressBar>();
            bar.RegisterValueChangedCallback(evt =>
            {
                bar.title = evt.newValue.ToString();
            });

        }
        #endregion

    }
}