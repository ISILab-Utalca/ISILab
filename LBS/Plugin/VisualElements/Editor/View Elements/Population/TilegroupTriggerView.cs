using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Data;
using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public partial class TilegroupTriggerView: VisualElement
    {
        #region VIEWS
        private readonly LBSCustomEnumField TriggerType;

        //box
        private readonly LBSCustomUnsignedIntegerField Range;

        private readonly LBSCustomToggleField Visible;

        private readonly ColorField ColorField;

        private readonly LBSCustomEnumField _modeSelector;

        private readonly LBSCustomEventHooker _hooker;
        #endregion

        #region FIELDS
        private readonly VisualTreeAsset visualTree;
        private TileTrigger trigger;

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
            _hooker.AllowChangeTriggerEnable = true;
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
                switch (trigger.activationMode)
                {
                    case TriggerActivationMode.OnEnter:
                        _hooker.EventType = LBSEventType.TriggerEnter;
                        break;
                    case TriggerActivationMode.OnExit:
                        _hooker.EventType = LBSEventType.TriggerExit;
                        break;
                    case TriggerActivationMode.OnStay:
                        _hooker.EventType = LBSEventType.TriggerStay; 
                        break;
                }
            });

            // Starting values
            _modeSelector.SetValueWithoutNotify(TriggerActivationMode.OnEnter);
            _hooker.EventType = LBSEventType.TriggerEnter;

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