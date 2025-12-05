using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using LBS;
using LBS.Bundles;
using LBS.Components;
using LBS.VisualElements;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using ISILab.DevTools.Macros;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Behaviours.Editor
{

    [LBSCustomEditor("Schema Behaviour", typeof(SchemaBehaviour))]
    public class SchemaBehaviourEditor : LBSCustomEditor, IToolProvider, IBundleFilter
    {

        #region FIELDS
        private SchemaBehaviour behaviour;

        private AddSchemaTile addSchemaTile;
        private RemoveSchemaTile removeSchemaTile;

        private AddSchemaTileConnection addTileConnection;
        private RemoveTileConnection removeTileConnection;
        #endregion

        #region VIEW FIELDS
        private VectorImage icon = Resources.Load<VectorImage>("Icons/Vectorial/Icon=Palette");
        private SimplePallete areaPallete;
        private SimplePallete connectionPallete;
        private string zoneIconGuid = "76bf813a38668ce439887addd209058c";
        private string windowConnectionIconGuid = "c0d00de1d82858c4b9d772a012caf67d";
        private string doorConnectionIconGuid = "cd77d8067cf8b6b44ab23da9a62173c0";
        private string wallConnectionIconGuid = "b29ab5d90498432409a5fb48f6be7bd5";
        private string emptyConnectionIconGuid = "072eebdede709814ea347b1cde4b51a2";

        #endregion

        #region PROPERTIES
        private Color BHcolor => LBSSettings.Instance.view.behavioursColor;

        public LBSButtonListFilter BundlePickerWindow { get; set; }
        #endregion

        #region CONSTRUCTORS

        public SchemaBehaviourEditor(object target) : base(target)
        {
            behaviour = target as SchemaBehaviour;
            CreateVisualElement();
        }


        #endregion

        #region METHODS
        public void SetTools(ToolKit toolKit)
        {
            addSchemaTile = new AddSchemaTile();
            var t1 = new LBSTool(addSchemaTile);
            t1.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;

            removeSchemaTile = new RemoveSchemaTile();
            var t2 = new LBSTool(removeSchemaTile);
            t2.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
            
            addTileConnection = new AddSchemaTileConnection();
            var t3 = new LBSTool(addTileConnection);
            t3.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
            
            removeTileConnection = new RemoveTileConnection();
            var t4 = new LBSTool(removeTileConnection);
            t4.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
            
            addSchemaTile.SetRemover(removeSchemaTile);
            addTileConnection.SetRemover(removeTileConnection);
            
            toolKit.ActivateTool(t1,behaviour.OwnerLayer, behaviour);
            toolKit.ActivateTool(t2,behaviour.OwnerLayer, behaviour);
            toolKit.ActivateTool(t3,behaviour.OwnerLayer, behaviour);
            toolKit.ActivateTool(t4,behaviour.OwnerLayer, behaviour);
            
            addSchemaTile.OnManipulationLeftClickCtrl += AddZone;
        }

        public override void SetInfo(object paramTarget)
        {
            behaviour = paramTarget as SchemaBehaviour;
            SetAreaPallete();
            SetConnectionPallete();
        }

        protected override VisualElement CreateVisualElement()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("SchemaBehaviourEditor");
            visualTree.CloneTree(this);

            // Area Pallete
            areaPallete = this.Q<SimplePallete>("ZonePallete");
            SetAreaPallete();

            // Inside Field
            var insideField = this.Q<LBSCustomObjectField>("InsideField");
            insideField.value = behaviour.PressetInsideStyle;
            insideField.RegisterValueChangedCallback(evt =>
            {
                behaviour.PressetInsideStyle = evt.newValue as Bundle;
            });
            insideField.UseCustomFilter = true;
            insideField.CustomFilter = InteriorFilter;

            // Outside Field
            var outsideField = this.Q<LBSCustomObjectField>("OutsideField");
            outsideField.value = behaviour.PressetOutsideStyle;
            outsideField.RegisterValueChangedCallback(evt =>
            {
                behaviour.PressetOutsideStyle = evt.newValue as Bundle;
            });
            outsideField.UseCustomFilter = true;
            outsideField.CustomFilter = InteriorFilter;

            // Connection Pallete
            connectionPallete = this.Q<SimplePallete>("ConnectionPallete");
            SetConnectionPallete();

            // Multi-layer connection toggle
            var multiLayerConnectionsToggle = this.Q<LBSCustomToggleField>("MultilayerConnections");
            multiLayerConnectionsToggle.RegisterValueChangedCallback(evt =>
            {
                behaviour.MultiLayerConnections = evt.newValue;
                if (evt.newValue)
                {
                    addTileConnection.MultiLayerSetup();
                    LBSMainWindow.MessageNotify("Multi-layer connection painting enabled.");
                }
                else LBSMainWindow.MessageNotify("Multi-layer connection painting disabled.");
            });
            multiLayerConnectionsToggle.SetValueWithoutNotify(behaviour.MultiLayerConnections);

            return this;

            void InteriorFilter(System.Action<Object> pick)
            {
                var bundles = BundleQueryUtility.FindBundlesWithCharacteristic<LBSMainInteriorBundle>(includeChildren: true);
                (this as IBundleFilter).OpenFilterWindow(bundles, picked => pick(picked));
            }
        }

        private void SetAreaPallete()
        {
            if (areaPallete == null)
            {
                Debug.Log("no pallete");
                return;
            }

            // Clear old event handlers to avoid duplicates
            areaPallete.ClearBindings();

            areaPallete.ShowGroups = false;
            areaPallete.SetName("Zones");
            areaPallete.SetIcon(icon, BHcolor);

            var zones = behaviour.Zones;
            var options = new object[zones.Count];
            for (int i = 0; i < zones.Count; i++)
            {
                options[i] = zones[i];
            }

            areaPallete.OnSelectOption += (selected) =>
            {
                behaviour.RoomToSet = selected as Zone;
                ToolKit.Instance.SetActive(typeof(AddSchemaTile));
            };

            areaPallete.OnAddOption -= AddZone;
            areaPallete.OnAddOption += AddZone;

            areaPallete.SetOptions(options, (optionView, option) =>
            {
                var area = (Zone)option;
                optionView.Label = area.ID;
                optionView.Color = area.Color;
                optionView.Icon = AssetMacro.LoadAssetByGuid<VectorImage>(zoneIconGuid);
            });

            areaPallete.OnRepaint += () =>
            {
                var refreshedZones = behaviour.Zones;
                areaPallete.Options = new object[refreshedZones.Count];
                for (int i = 0; i < refreshedZones.Count; i++)
                {
                    areaPallete.Options[i] = refreshedZones[i];
                }

                areaPallete.Selected = behaviour.RoomToSet;
            };

            areaPallete.OnRemoveOption += (option) =>
            {
                if (option == null) return;

                var answer = EditorUtility.DisplayDialog("Caution",
                    "You are about to delete a zone, which may be related" +
                    " to tiles on your map. If you delete the zone," +
                    " the corresponding tiles will also be removed." +
                    " Are you sure you want to proceed?", "Continue", "Cancel");

                if (!answer) return;

                behaviour.RemoveZone(option as Zone);
                DrawManager.Instance.RedrawLayer(behaviour.OwnerLayer);
                ToolKit.Instance.SetActive(typeof(AddSchemaTile));
                areaPallete.Repaint();
            };
            
            areaPallete.Repaint(); 
        }


        private void AddZone()
        {
            var newZone = behaviour.AddZone();
            newZone.InsideStyles = new List<string>() { behaviour.PressetInsideStyle.Name };
            newZone.OutsideStyles = new List<string>() { behaviour.PressetOutsideStyle.Name };
            areaPallete.Options = new object[behaviour.Zones.Count];
            for (int i = 0; i < behaviour.Zones.Count; i++)
            {
                areaPallete.Options[i] = behaviour.Zones[i];
             
            }
            behaviour.RoomToSet = newZone;
            areaPallete.Repaint();
        }

        private void SetConnectionPallete()
        {
            connectionPallete.ShowGroups = false;
            connectionPallete.ShowRemoveButton = false;
            connectionPallete.ShowAddButton = false;
            connectionPallete.ShowNoElement = false;
            
            connectionPallete.SetName("Connections");
            connectionPallete.SetIcon(icon, BHcolor);
            
            var connections = SchemaBehaviour.Connections;
            var options = new object[connections.Count];
            for (int i = 0; i < connections.Count; i++)
            {
                options[i] = connections[i];
            }
            
            // Select option event
            connectionPallete.OnSelectOption += (selected) =>
            {
                // var tk = ToolKit.Instance;
                behaviour.conectionToSet = selected as string;
                //setTileConnection.ToSet = selected as string;
                ToolKit.Instance.SetActive(typeof(AddSchemaTileConnection));
            };

            // Init options
            connectionPallete.SetOptions(options, (optionView, option) =>
            {
                var arg1Label = (string)option;
                optionView.Label = arg1Label;
                optionView.Icon = GetOptionIcon(arg1Label);

            });
            
            
            connectionPallete.OnRepaint += () => { connectionPallete.Selected = behaviour.conectionToSet; };
            connectionPallete.Repaint();
        }

        VectorImage GetOptionIcon(string label)
        {
            if(label == "Empty") return AssetMacro.LoadAssetByGuid<VectorImage>(emptyConnectionIconGuid);
            if(label == "Wall") return AssetMacro.LoadAssetByGuid<VectorImage>(wallConnectionIconGuid);
            if(label == "Door") return AssetMacro.LoadAssetByGuid<VectorImage>(doorConnectionIconGuid);
            if(label == "Window") return AssetMacro.LoadAssetByGuid<VectorImage>(windowConnectionIconGuid);
            return AssetMacro.LoadAssetByGuid<VectorImage>(zoneIconGuid);
        }
        
        #endregion
    }
}