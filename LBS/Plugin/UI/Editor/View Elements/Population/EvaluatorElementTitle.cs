using ISILab.LBS.AI.Categorization;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using ISILab.LBS.Plugin.Core.Settings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.View_Elements.Population.EvaluatorElementTitle
{
    /// <summary>
    /// Visual element used to show evaluators in a list.
    /// </summary>
    [UxmlElement]
    public partial class EvaluatorElementTitle : LBSComplexVisualElement
    {
        private LBSCustomLabel evLabel;
        private LBSCustomLabel evLabel2;
        private LBSCustomLabel evLabel3;


        public string EvLabelString
        {
            get => evLabel.text;
            private set => evLabel.text = value;
        }

        public string EvLabelString2
        {
            get => evLabel2.text;
            private set => evLabel2.text = value;
        }

        public string EvLabelString3
        {
            get => evLabel3.text;
            private set => evLabel3.text = value;
        }

        public EvaluatorElementTitle() : base()
        {
            InitInternal();
        }

        public EvaluatorElementTitle(string label ,string label2 ,string label3 )
        {
            InitInternal();
            SetEvaluatorElement(label, label2, label3);
        }

        private void InitInternal()
        {
            GetVisualTreeForThis();

            evLabel = this.Q<LBSCustomLabel>("evName");
            evLabel2 = this.Q<LBSCustomLabel>("evName2");
            evLabel3= this.Q<LBSCustomLabel>("evName3");
        }

        public void SetEvaluatorElement(string label, string label2 ,string label3)
        {
            EvLabelString = label;
            EvLabelString2 = label2;
            EvLabelString3 = label3;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
