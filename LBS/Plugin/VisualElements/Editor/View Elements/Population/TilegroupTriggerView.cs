using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Modules;
using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public partial class TilegroupTriggerView: VisualElement
    {
        #region VIEWS
        LBSCustomEnumField TriggerType;

        //box
        LBSCustomUnsignedIntegerField Range;

        LBSCustomToggleField Visible;

        ColorField ColorField;
        #endregion

        #region FIELDS
        VisualTreeAsset visualTree;
        TileTrigger trigger;
        private LBSCustomEventHooker _hooker;
        #endregion

        #region PROPERTIES
        public TileTrigger Trigger
        {
            get { return trigger; }
            set
            {
                trigger = value;
                UpdateByTrigger(trigger);
            }
        }

        public Action<TileTriggerType> OnTriggerTypeChanged { get; internal set; }


        #endregion

        #region CONSTRUCTORS

        public TilegroupTriggerView()
        {
            if (visualTree is null)
            {
                visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("TilegroupTriggerView", true);
            }

            visualTree.CloneTree(this);

            TriggerType = this.Q<LBSCustomEnumField>("TriggerType");
            var ef = TriggerType as EnumField;
            ef.dataSourceType = typeof(TileTriggerType);

            _hooker = this.Q<LBSCustomEventHooker>("EventHooker");
            _hooker.Selector.RegisterValueChangedCallback(evt =>
            {
                QuestActionData data = GetSelectedNodeData();
                if (data is null) return;
                data.Target = evt.newValue as GameObject;
                _hooker.RefreshMethodList();
            });
            _hooker.Selector.allowSceneObjects = true;

            Range = this.Q<LBSCustomUnsignedIntegerField>("Range");
 
            Visible = this.Q<LBSCustomToggleField>("Visible");
            ColorField = this.Q<ColorField>("Color");
            RegisterCallbacks();
        }

        #endregion

        #region METHODS
        private void RegisterCallbacks()
        {
            Visible.RegisterValueChangedCallback(evt =>
            {
                trigger.isVisible = evt.newValue;
                UpdateTileByCurrentType();
            });

            ColorField.RegisterValueChangedCallback(evt =>
            {
                trigger.areaColor = evt.newValue;
                UpdateTileByCurrentType();
            });

            TriggerType.RegisterValueChangedCallback(evt => 
            {
                TileTriggerType newType = (TileTriggerType)evt.newValue;
                OnTriggerTypeChanged?.Invoke((TileTriggerType)TriggerType.value);
            });

            Range.RegisterValueChangedCallback(evt => 
            {                
                if(trigger is TileBoxTrigger tbt)
                {
                    if(tbt.Length != evt.newValue)
                    {
                        tbt.Length = evt.newValue;
                        UpdateTileByCurrentType();
                    }          
                }
                else if (trigger is TileCircleTrigger tct)
                {
                    if (tct.Radius != evt.newValue)
                    {
                        tct.Radius = evt.newValue;
                        UpdateTileByCurrentType();
                    }                 
                }
            });

        }

        private void UpdateTileByCurrentType()
        {
            OnTriggerTypeChanged?.Invoke((TileTriggerType)TriggerType.value);
        }

        private void UpdateByTrigger(TileTrigger trigger)
        {
            TileTriggerType type = TileTrigger.GetType(trigger.GetType());
            TriggerType.SetValueWithoutNotify(type);
            ColorField.SetValueWithoutNotify(trigger.areaColor);
            Visible.SetValueWithoutNotify(trigger.isVisible);

            if (trigger is TileBoxTrigger tbt) Range.SetValueWithoutNotify((uint)tbt.Length);
        }

   
        #endregion
    }

}