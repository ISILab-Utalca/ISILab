using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class AddonTriggerConnectionView : VisualElement
    {
        #region FIELDS

        TriggerUnlockEntry entry;

        #endregion

        #region VIEW FIELDS

        LBSCustomEnumField triggerType;
        LBSCustomListView addonList;
  
        static VisualTreeAsset visualTree { get; set; }
        public TriggerUnlockEntry Entry { get => entry;

            set
            {
                entry = value;
                SetList();
            }
        }

        #endregion

        #region Constructors
        public AddonTriggerConnectionView()
        {
            LoadVisualElement();
        }

        #endregion

        #region METHODS

        protected void LoadVisualElement()
        {
            if (visualTree is null)
            {
                visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("AddonTriggerConnectionView", true);
            }

            visualTree.CloneTree(this);

            addonList = this.Q<LBSCustomListView>("AddonList");
            triggerType = this.Q<LBSCustomEnumField>("TriggerType");

            triggerType.dataSourceType = typeof(TriggerActivationMode);

            triggerType.RegisterValueChangedCallback(evt =>
            {
                if (entry == null) return;

                entry.Mode = (TriggerActivationMode)evt.newValue;
            });

        }

        private void SetList()
        {
            if (entry == null) return;

            triggerType.SetValueWithoutNotify(entry.Mode);

            addonList.itemsSource = entry.Unlocks;
            addonList.makeItem = () => new AddonConnectionView();
            addonList.bindItem = (el, i) =>
            {
                if (entry.Unlocks[i] is null) entry.Unlocks[i] = new Addon_Unlock();
                ((AddonConnectionView)el).SetInfo(entry.Unlocks[i]);


            };
        }
        #endregion
    }
}