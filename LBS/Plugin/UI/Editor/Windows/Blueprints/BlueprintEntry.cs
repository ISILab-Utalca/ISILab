using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint
{
    [UxmlElement]
    public partial class BlueprintEntry : VisualElement
    {
        #region VIEW ELEMENTS
        LBSCustomLabelIcon defaultMessage;
        VisualElement blueprintImage;
        ISILab.LBS.Components.Blueprint blueprint;
        #endregion

        #region FIELDS
        private static VisualTreeAsset visualTreeAsset;
        #endregion

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
        }

        #endregion
    }
}
