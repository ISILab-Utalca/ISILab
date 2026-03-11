using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Behaviours;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint
{
    [UxmlElement]
    public partial class BlueprintEntry : VisualElement
    {
        #region VIEW ELEMENTS
        LBSCustomLabel blueprintLabel;
        LBSCustomLabelIcon defaultMessage;
        VisualElement blueprintImage;
        #endregion

        #region FIELDS
        private static VisualTreeAsset visualTreeAsset;
        ISILab.LBS.Components.Blueprint blueprint;
        #endregion

        public Action OnSelect;

        #region PROPERTIES
        internal Texture2D BlueprintImage
        {

            set
            {
                defaultMessage.style.display = value == null ? DisplayStyle.Flex : DisplayStyle.None;
                blueprintImage.style.display = value != null ? DisplayStyle.Flex : DisplayStyle.None;

                blueprintImage.style.backgroundImage = value;
            }
        }

        public ISILab.LBS.Components.Blueprint Blueprint
        {
            get
            {
                return blueprint;
            }

            internal set
            {
                blueprint = value;
                BlueprintImage = blueprint.PreviewImage;
                blueprintLabel.text = blueprint.BlueprintName;
                tooltip = MakeTooltip();
            }
        }


        #endregion

        #region CONSTRUCTORS

        public BlueprintEntry() : base()
        {
            visualTreeAsset ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("BlueprintEntry");
            visualTreeAsset.CloneTree(this);
            
            defaultMessage = this.Q<LBSCustomLabelIcon>("DefaultMessage");
            blueprintImage = this.Q<VisualElement>("BlueprintImage");
            blueprintLabel = this.Q<LBSCustomLabel>("BlueprintName");
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }


        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                OnSelect?.Invoke();
                SetSelected(true);
            }
        }

        public void SetSelected(bool value)
        {
            if (value)
            {
                AddToClassList("prop-state--checked");
            }
            else
            {
                RemoveFromClassList("prop-state--checked");
            }
        }
        private string MakeTooltip()
        {
            if (blueprint == null)
                return "Empty blueprint slot";

            Dictionary<Type, string> dict = new()
            {
                { typeof(SchemaBehaviour), "Interior Layer" },
                { typeof(ExteriorBehaviour), "Exterior Layer" },
                { typeof(PopulationBehaviour), "Population Layer" },
                { typeof(QuestBehaviour), "Quest Layer" },
            };

            HashSet<Type> registeredTypes = new();

            string tooltip = "Layers:\n";

            foreach (BlueprintStorable storable in blueprint.StorableData)
            {
                if (!storable.Data.Any()) continue;

                foreach (BlueprintData entry in storable.Data)
                {
                    if (entry.Object == null) continue;

                    Type type = entry.Object.GetType();

                    if (!registeredTypes.Contains(type) && dict.ContainsKey(type))
                    {
                        registeredTypes.Add(type);
                        tooltip += $"- {dict[type]}\n";
                    }
                }
            }

            return tooltip;
        }

        #endregion
    }
}
