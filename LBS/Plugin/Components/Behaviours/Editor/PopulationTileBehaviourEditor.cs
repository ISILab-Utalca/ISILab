using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Editor;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("PopulationTileBehaviour", typeof(PopulationTileBehaviour))]
    public class PopulationTileBehaviourEditor : LBSCustomEditor
    {
        #region FIELDS

        private PopulationTileBehaviour behaviour;
        
        #region VIEW FIELDS

        public LBSButtonListFilter BundlePickerWindow { get; set; }
        #endregion

        #endregion

        #region CONSTRUCTORS
        public PopulationTileBehaviourEditor(object target) : base(target)
        {
            behaviour = target as PopulationTileBehaviour;
            if (behaviour is null) return;
  

            SetInfo(behaviour);
            CreateVisualElement();
        }
        #endregion
        
        #region METHODS
        public sealed override void SetInfo(object paramTarget)
        {
            behaviour = paramTarget as PopulationTileBehaviour;
        }

        protected sealed override VisualElement CreateVisualElement()
        {
            
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("PopulationTileBehaviourEditor");
            visualTree.CloneTree(this);
            
          
            UpdateTilebundle();
            return this;
        }

        private void UpdateTilebundle()
        {
            // Set init options

            
        }

        public override void OnUnfocus()
        {
            base.OnUnfocus();
        }

        #endregion
    }
}