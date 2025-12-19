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

        LBSCustomEnumField _modeSelector;
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
                if (Trigger is null) return;
                _hooker.Hooker = Trigger._eventHooker;
             
            });
            _hooker.Selector.allowSceneObjects = true;

            Range = this.Q<LBSCustomUnsignedIntegerField>("Range");

            _modeSelector = this.Q<LBSCustomEnumField>("ModeSelector");
            _modeSelector.RegisterValueChangedCallback(evt =>
            {
                if (trigger is null) return;
                trigger.activationMode = (TriggerActivationMode)evt.newValue;
            });
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
                trigger.Ttype = (TileTriggerType)evt.newValue;
                OnTriggerTypeChanged?.Invoke((TileTriggerType)TriggerType.value);
            });

            Range.RegisterValueChangedCallback(evt => 
            {
                if (trigger.Range == evt.newValue) return;
                trigger.Range = evt.newValue;
                UpdateTileByCurrentType();

            });

        }

        private void UpdateTileByCurrentType()
        {
            OnTriggerTypeChanged?.Invoke((TileTriggerType)TriggerType.value);
        }

        private void UpdateByTrigger(TileTrigger trigger)
        {
            TriggerType.SetValueWithoutNotify(trigger.Ttype);
            ColorField.SetValueWithoutNotify(trigger.areaColor);
            Visible.SetValueWithoutNotify(trigger.isVisible);
            Range.SetValueWithoutNotify((uint)trigger.Range);
            _modeSelector.SetValueWithoutNotify(trigger.activationMode);

            if (Trigger is null) return;
            _hooker.Hooker = Trigger._eventHooker;
        }

   
        #endregion
    }

}