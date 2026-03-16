using ISILab.LBS.AI.Categorization;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using UnityEngine.UIElements;
using static ISILab.LBS.AI.Categorization.EvaluatorConfiguration;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("EvaluatorConfiguration", typeof(EvaluatorConfiguration))]
    public class EvaluatorConfigurationVE : LBSCustomEditor
    {
        LBSCustomListView fieldList;

        LBSCustomButton defaultButton;

        public EvaluatorConfigurationVE(object target) : base(target)
        {
            CreateVisualElement();
            SetInfo(target);
        }

        public override void SetInfo(object paramTarget)
        {
            
        }

        protected override VisualElement CreateVisualElement()
        {
            var config = target as EvaluatorConfiguration;

            fieldList = new LBSCustomListView();
            fieldList.headerTitle = "Parameters";
            fieldList.showFoldoutHeader = true;
            fieldList.showBoundCollectionSize = false;
            fieldList.itemsSource = config.fields;
            fieldList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            fieldList.makeItem = () => new VisualElement();
            fieldList.bindItem = (item, i) =>
            {
                item.Clear();
                item.Add((fieldList.itemsSource[i] as EvaluatorConfigurationField).GetField());
            };
            Add(fieldList);

            defaultButton = new LBSCustomButton();
            defaultButton.style.height = 30;
            defaultButton.text = "Default";
            defaultButton.clicked -= (config.target as IEvaluator).InitializeDefault;
            defaultButton.clicked += (config.target as IEvaluator).InitializeDefault;
            Add(defaultButton);

            return this;
        }
    }
}

