using ISILab.AI.Categorization;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("DCExploration", typeof(DCExploration))]
    public class DCExplorationVE : LBSCustomEditor
    {
        //DCExploration evaluator;

        LBSCustomButton configurationButton;

        //LBSCustomObjectField playerTagField;
        //LBSCustomObjectField obstacleTagField;
        //LBSCustomListView POIsList;

        //LBSCustomButton setDefaultButton;

        //LBSTag EvaluatorPlayerTag => (evaluator.playerCharacteristic as LBSTagsCharacteristic).TagEntries[0].Value;
        //LBSTag EvaluatorObstacleTag => (evaluator.colliderCharacteristic as LBSTagsCharacteristic).TagEntries[0].Value;

        public DCExplorationVE(object target) : base(target)
        {
            CreateVisualElement();
            SetInfo(target);
        }

        public override void SetInfo(object paramTarget)
        {
            return;

            //evaluator = paramTarget as DCExploration;

            //if (evaluator is null) return;

            //if (evaluator.playerCharacteristic is not null)
            //{
            //    playerTagField.value = EvaluatorPlayerTag;
            //}
            //playerTagField.RegisterValueChangedCallback(evt =>
            //{
            //    evaluator.playerCharacteristic = new LBSTagsCharacteristic(evt.newValue as LBSTag);
            //});
            
            //if (evaluator.colliderCharacteristic is not null)
            //{
            //    obstacleTagField.value = EvaluatorObstacleTag;
            //}
            //obstacleTagField.RegisterValueChangedCallback(evt =>
            //{
            //    evaluator.colliderCharacteristic = new LBSTagsCharacteristic(evt.newValue as LBSTag);
            //});

            //if(evaluator.pointsOfInterest is not null)
            //{
            //    POIsList.itemsSource = evaluator.pointsOfInterest;
            //}
        }

        protected override VisualElement CreateVisualElement()
        {
            configurationButton = new LBSCustomButton();
            configurationButton.style.height = 30;
            configurationButton.text = "Advanced configuration";
            configurationButton.clicked -= ShowConfiguration;
            configurationButton.clicked += ShowConfiguration;
            Add(configurationButton);

            static void ShowConfiguration() => Selection.activeObject = DCExploration.config;

            return this;

            //playerTagField = new LBSCustomObjectField();
            //playerTagField.label = "Player Tag";
            //playerTagField.objectType = typeof(LBSTag);
            //this.Add(playerTagField);

            //obstacleTagField = new LBSCustomObjectField();
            //obstacleTagField.label = "Obstacle Tag";
            //obstacleTagField.objectType = typeof(LBSTag);
            //this.Add(obstacleTagField);

            //POIsList = new LBSCustomListView();
            //POIsList.headerTitle = "POIs";
            //POIsList.showAddRemoveFooter = true;
            //POIsList.makeItem = () =>
            //{
            //    var field = new LBSCustomObjectField();
            //    field.objectType = typeof(LBSTag);
            //    field.style.marginLeft = 10;
            //    field.RegisterValueChangedCallback(evt =>
            //    {
            //        int index = evaluator.pointsOfInterest.FindIndex(cha => 
            //        {
            //            var tagsChar = cha as LBSTagsCharacteristic;
            //            return tagsChar is not null && tagsChar.HasTag(evt.previousValue as LBSTag);
            //        });
                    
            //        if(index != -1)
            //        {
            //            evaluator.pointsOfInterest.RemoveAt(index);
            //            evaluator.pointsOfInterest.Insert(index, new LBSTagsCharacteristic(evt.newValue as LBSTag));
            //        }
            //        else
            //        {
            //            evaluator.pointsOfInterest = new System.Collections.Generic.List<LBSCharacteristic>(evaluator.pointsOfInterest.Where(p => p != null));
            //            POIsList.itemsSource = evaluator.pointsOfInterest;

            //            evaluator.pointsOfInterest.Add(new LBSTagsCharacteristic(evt.newValue as LBSTag));
            //            //Debug.Log(POIsList.itemsSource.Count);
            //        }

            //        POIsList.RefreshItems();
            //        POIsList.Rebuild();
            //    });
            //    return field;
            //};
            //POIsList.bindItem = (item, i) =>
            //{
            //    if (evaluator is null || evaluator.pointsOfInterest.Count == 0) return;
            //    var tagChar = evaluator.pointsOfInterest[i] as LBSTagsCharacteristic;
            //    if (tagChar is null || tagChar.TagEntries.Count == 0) return;
            //    var entry = tagChar.TagEntries[0];
            //    if (entry is null) return;

            //    (item as LBSCustomObjectField).SetValueWithoutNotify(entry.Value);
            //};
            //this.Add(POIsList);

            //setDefaultButton = new LBSCustomButton();
            //setDefaultButton.style.height = 30;
            //setDefaultButton.text = "Default";
            //setDefaultButton.clicked -= SetDefault;
            //setDefaultButton.clicked += SetDefault;
            //this.Add(setDefaultButton);

            //return this;

            //void SetDefault()
            //{
            //    evaluator.InitializeDefault();
            //    playerTagField.value = EvaluatorPlayerTag;
            //    obstacleTagField.value = EvaluatorObstacleTag;
            //    POIsList.RefreshItems();
            //    POIsList.Rebuild();
            //}
        }
    }
}

//string s = "";
//foreach (LBSCharacteristic poi in evaluator.pointsOfInterest)
//{
//    s += $"\n{poi?.FirstTag().label}";
//}
//Debug.Log(s);