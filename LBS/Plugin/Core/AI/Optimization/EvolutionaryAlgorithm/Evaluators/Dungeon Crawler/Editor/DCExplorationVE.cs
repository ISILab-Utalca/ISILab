using ISILab.AI.Categorization;
using ISILab.Extensions;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("DCExploration", typeof(DCExploration))]
    public class DCExplorationVE : LBSCustomEditor
    {
        LBSCustomObjectField playerTagField;
        LBSCustomObjectField obstacleTagField;
        LBSCustomListView POIsList;

        Button setDefaultButton;

        public DCExplorationVE(object target) : base(target)
        {
            CreateVisualElement();
            SetInfo(target);
        }

        public override void SetInfo(object paramTarget)
        {
            var eval = paramTarget as DCExploration;
            this.target = eval;

            if (eval is null) return;
            //eval.InitializeDefault();

            if (eval.playerCharacteristic is not null)
            {
                playerTagField.value = (eval.playerCharacteristic as LBSTagsCharacteristic).TagEntries[0].Value;
            }
            playerTagField.RegisterValueChangedCallback(evt =>
            {
                eval.playerCharacteristic = new LBSTagsCharacteristic(playerTagField.value as LBSTag);
            });
            
            if (eval.colliderCharacteristic is not null)
            {
                obstacleTagField.value = (eval.colliderCharacteristic as LBSTagsCharacteristic).TagEntries[0].Value;
            }
            obstacleTagField.RegisterValueChangedCallback(evt =>
            {
                eval.colliderCharacteristic = new LBSTagsCharacteristic(obstacleTagField.value as LBSTag);
            });

            if(eval.pointsOfInterest is not null)
            {
                POIsList.itemsSource = eval.pointsOfInterest;
            }
        }

        protected override VisualElement CreateVisualElement()
        {
            var eval = target as DCExploration;

            playerTagField = new LBSCustomObjectField();
            playerTagField.label = "Player Tag";
            playerTagField.objectType = typeof(LBSTag);
            this.Add(playerTagField);

            obstacleTagField = new LBSCustomObjectField();
            obstacleTagField.label = "Obstacle Tag";
            obstacleTagField.objectType = typeof(LBSTag);
            this.Add(obstacleTagField);

            POIsList = new LBSCustomListView();
            POIsList.headerTitle = "POIs";
            POIsList.showAddRemoveFooter = true;
            POIsList.makeItem = () =>
            {
                //var eval = target as DCExploration;
                var field = new LBSCustomObjectField();
                field.objectType = typeof(LBSTag);
                field.style.marginLeft = 10;
                field.RegisterValueChangedCallback(evt =>
                {
                    int index = eval.pointsOfInterest.FindIndex(cha => 
                    {
                        var tagsChar = cha as LBSTagsCharacteristic;
                        return tagsChar is not null && tagsChar.HasTag(evt.previousValue as LBSTag);
                    });
                    
                    if(index != -1)
                    {
                        eval.pointsOfInterest.RemoveAt(index);
                        eval.pointsOfInterest.Insert(index, new LBSTagsCharacteristic(evt.newValue as LBSTag));
                    }
                    else
                    {
                        eval.pointsOfInterest = new System.Collections.Generic.List<LBSCharacteristic>(eval.pointsOfInterest.Where(p => p != null));
                        POIsList.itemsSource = eval.pointsOfInterest;
                        eval.pointsOfInterest.Add(new LBSTagsCharacteristic(evt.newValue as LBSTag));
                        Debug.Log(POIsList.itemsSource.Count);

                    }

                    POIsList.RefreshItems();
                    POIsList.Rebuild();

                    string s = "";
                    foreach(LBSCharacteristic poi in eval.pointsOfInterest)
                    {
                        s += $"\n{poi?.FirstTag().label}";
                    }
                    Debug.Log(s);
                });
                return field;
            };
            POIsList.bindItem = (item, i) =>
            {
                //var eval = target as DCExploration;
                if (eval is null || eval.pointsOfInterest.Count == 0) return;
                var tagChar = eval.pointsOfInterest[i] as LBSTagsCharacteristic;
                if (tagChar is null || tagChar.TagEntries.Count == 0) return;
                var entry = tagChar.TagEntries[0];
                if (entry is null) return;

                (item as LBSCustomObjectField).SetValueWithoutNotify(entry.Value);
            };
            this.Add(POIsList);

            setDefaultButton = new LBSCustomButton();
            //setDefaultButton.style.backgroundColor = Color.red;
            //setDefaultButton.style.width = 100;
            setDefaultButton.style.height = 30;
            //setDefaultButton.style.minHeight = 30;
            setDefaultButton.text = "Default";
            setDefaultButton.clicked -= SetDefault;
            setDefaultButton.clicked += SetDefault;
            //var ve = new VisualElement();
            //ve.style.height = 32;
            //ve.style.flexGrow = 0;
            //this.Add(ve);
            //ve.Add(setDefaultButton);
            this.Add(setDefaultButton);

            return this;

            void SetDefault()
            {
                eval.InitializeDefault();
                POIsList.RefreshItems();
                POIsList.Rebuild();
            }
        }
    }
}

