using ISILab.AI.Categorization;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("DCExploration", typeof(DCExploration))]
    public class DCExplorationVE : LBSCustomEditor
    {
        LBSCustomObjectField playerTagField;
        LBSCustomObjectField obstacleTagField;
        ListView POIsList;

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
            eval.InitializeDefault();

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
            playerTagField = new LBSCustomObjectField();
            playerTagField.label = "Player Tag";
            playerTagField.dataSourceType = typeof(LBSTag);
            this.Add(playerTagField);

            obstacleTagField = new LBSCustomObjectField();
            obstacleTagField.label = "Obstacle Tag";
            obstacleTagField.dataSourceType = typeof(LBSTag);
            this.Add(obstacleTagField);

            POIsList = new ListView();
            POIsList.makeItem = () =>
            {
                var field = new LBSCustomObjectField();
                field.dataSourceType = typeof(LBSTag);
                return field;
            };
            POIsList.bindItem = (item, i) =>
            {
                (item as LBSCustomObjectField).value = ((target as DCExploration).pointsOfInterest[i] as LBSTagsCharacteristic).TagEntries[0].Value;
            };
            POIsList.itemsAdded += i =>
            {
                // TODO
            };
            this.Add(POIsList);

            return this;
        }
    }
}

