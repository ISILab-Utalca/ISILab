using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Internal;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.Settings;
using LBS;
using LBS.Bundles;
using LBS.Components;
using LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Internal;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("Exterior Behaviour", typeof(ExteriorBehaviour))]
    public class ExteriorBehaviourEditor : LBSCustomEditor, IToolProvider, IBundleFilter
    {
        #region FIELDS
        private ExteriorBehaviour exterior;
   
        private List<LBSTagGroup> Groups;
        private object[] options;

        private AddExteriorTile addExteriorTile;
        private AddVertexExteriorTile addVertexExteriorTile;
        private RemoveTileExterior removeTile;
        private SetExteriorTileConnection setConnection;
        private SetVertexExteriorTileConnection setVertexConnection;
        private RemoveConnectionInArea removeConnectionInArea;
        #endregion

        #region VIEW FIELDS
        private VectorImage icon = LBSAssetMacro.LoadAssetByGuid<VectorImage>("87f2bb6f2c78b184a8ea2b6a5b14f878");
        private SimplePallete connectionPallete;
        private LBSCustomObjectField bundleField;
        private WarningPanel warningPanel;
        private string tileIconGuid = "";
        #endregion

        #region PROPERTIES
        public LBSButtonListFilter BundlePickerWindow { get; set; }
        
        private Color BHcolor => LBSSettings.Instance.view.behavioursColor;

        public ConnectedTileMapModule.ConnectedTileType GridType => exterior.GridType;
        #endregion

        #region CONSTRUCTORS
        public ExteriorBehaviourEditor(object target) : base(target)
        {
            // Set target Behaviour
            exterior = target as ExteriorBehaviour;
            
            //SetInfo(target);
            
            CreateVisualElement();
        }
        #endregion

        #region METHODS
        public sealed override void SetInfo(object paramTarget)
        {
            exterior = paramTarget as ExteriorBehaviour;
            CheckTargetBundle();
        }

        public void SetTools(ToolKit toolKit)
        {
            // We set the remover tool first as we want to avoid using switch statement twice when setting the add tool's remover.
            removeTile = new RemoveTileExterior();
            LBSTool t2 = new LBSTool(removeTile);

            addExteriorTile = new AddExteriorTile();
            addVertexExteriorTile = new AddVertexExteriorTile();

            setConnection = new SetExteriorTileConnection();
            setVertexConnection = new SetVertexExteriorTileConnection();

            LBSTool t1 = null, t3 = null;
            switch(GridType)
            {
                case ConnectedTileMapModule.ConnectedTileType.EdgeBased:
                    t1 = new LBSTool(addExteriorTile);
                    addExteriorTile.SetRemover(removeTile);
                    t3 = new LBSTool(setConnection);
                    break;
                case ConnectedTileMapModule.ConnectedTileType.VertexBased:
                    t1 = new LBSTool(addVertexExteriorTile);
                    addVertexExteriorTile.SetRemover(removeTile);
                    t3 = new LBSTool(setVertexConnection);
                    break;
            }

            foreach(LBSTool tool in new[] { t1, t2, t3 })
            {
                tool.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
                toolKit.ActivateTool(tool, exterior.OwnerLayer, exterior);
            }
        }

        private void CheckTargetBundle() 
        {
            var exteriorBundle = exterior.Bundle;
            if ( exteriorBundle == null)
            {
                warningPanel.SetDisplay(true);
                connectionPallete.SetDisplay(false);
            }
            else
            {
                warningPanel.SetDisplay(false);
                connectionPallete.SetDisplay(true);
                SetConnectionPallete(exterior.Bundle);
            }
        }

        protected sealed override VisualElement CreateVisualElement()
        {
            
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("ExteriorBehaviourEditor");
            visualTree.CloneTree(this);

            // WarningPanel
            warningPanel = this.Q<WarningPanel>();

            // BundleField
            bundleField = this.Q<LBSCustomObjectField>("BundleField");
            bundleField.label = "Exterior Tile Bundle";
            bundleField.objectType = typeof(Bundle);
            bundleField.value = exterior.Bundle;
            bundleField.UseCustomFilter = true;

            bundleField.CustomFilter = pick =>
            {
                var bundles = BundleQueryUtility.FindBundlesWithCharacteristic<LBSMainExteriorBundle>(includeChildren: true);
                (this as IBundleFilter).OpenFilterWindow(bundles, picked => pick(picked));
            };

            // only updates the first bundle value change - fix pending
            bundleField.RegisterValueChangedCallback(evt =>
            {
                var bundle = evt.newValue as Bundle;

                System.Action invalidBundleAction = () =>
                {
                    bundleField.value = exterior.Bundle;
                    LBSMainWindow.MessageNotify("Selected bundle was invalid.", LogType.Warning);
                };

                if(bundle)
                {
                    var identifierTags = LBSAssetsStorage.Instance.Get<LBSTag>();
                    var idents = SetPalleteConnectionView(bundle, identifierTags);

                    if (idents.Any())
                    {
                        exterior.Bundle = bundle; // valid for exterior
                        var owner = exterior.OwnerLayer;
                        owner.OnChangeUpdate(); // updates the assistant and viceversa
                    }
                    else
                    {
                        invalidBundleAction(); // set default or current if new option not valid
                    }
                }
                else
                {
                    invalidBundleAction(); // set default or current if new option not valid
                }

                CheckTargetBundle();
            });

            // Connection Pallete
            connectionPallete = this.Q<SimplePallete>("ConnectionPallete");
            CheckTargetBundle();

            exterior.OwnerLayer.OnChange += () =>
            {
                bundleField.SetValueWithoutNotify(exterior.Bundle);
                CheckTargetBundle();
            };
            
            return this;
        }

        public override void OnUnfocus()
        {
            base.OnUnfocus();
            (this as IBundleFilter).CloseFilterWindow();
        }

        private void SetConnectionPallete(Bundle bundle)
        {
            if (bundle == null) return;

            connectionPallete.style.display = DisplayStyle.Flex;
            
            // Set init options
            connectionPallete.ShowGroups = true;
            connectionPallete.ShowAddButton = false;
            connectionPallete.ShowRemoveButton = false;
            connectionPallete.ShowDropdown = false;
            connectionPallete.ShowNoElement = false;
            
            // Set basic value
            connectionPallete.SetName("Tile Brushes");
            connectionPallete.SetIcon(icon, BHcolor);
            
            var identifierTags = LBSAssetsStorage.Instance.Get<LBSTag>();
            var idents = SetPalleteConnectionView(bundle, identifierTags);

            exterior.identifierToSet = idents[0];
            
            // Selected option event
            connectionPallete.OnSelectOption += (selected) =>
            {
                exterior.identifierToSet = selected as LBSTag;
                // by default set the 
                System.Type activeManipulator = ToolKit.Instance.GetActiveManipulator().GetType();
                System.Type addToolType = null, connectionToolType = null;
                switch(GridType)
                {
                    case ConnectedTileMapModule.ConnectedTileType.EdgeBased:
                        addToolType = addExteriorTile.GetType();
                        connectionToolType = setConnection.GetType();
                        break;
                    case ConnectedTileMapModule.ConnectedTileType.VertexBased:
                        addToolType = addVertexExteriorTile.GetType();
                        connectionToolType = setVertexConnection.GetType();
                        break;
                }

                if (activeManipulator != addToolType &&
                     activeManipulator != connectionToolType)
                {
                    ToolKit.Instance.SetActive(addToolType);
                }
            };

            // Init options
            connectionPallete.SetOptions(options, (optionView, option) =>
            {
                var identifier = option as LBSTag;
                optionView.Label = identifier.Label;
                optionView.Color = identifier.Color;
                optionView.Icon = LBSAssetMacro.LoadAssetByGuid<VectorImage>(tileIconGuid);
                // optionView.Icon = identifier.Icon;
            });

            connectionPallete.OnRepaint += () => { connectionPallete.Selected = exterior.identifierToSet; };

            connectionPallete.Repaint();
        }

        private List<LBSTag> SetPalleteConnectionView(Bundle bundle, List<LBSTag> identifierTags)
        {
            var connections = bundle.GetChildrenCharacteristics<LBSDirection>();
            var tags = connections.SelectMany(c => c.Connections).ToList().RemoveDuplicates();
            //if (tags.Remove("Empty"))  tags.Insert(0, "Empty");
            tags.Remove("Empty"); tags.Insert(0, "Empty");
            var idents = tags.Select(s => identifierTags.Find(i => s == i.Label)).ToList().RemoveEmpties();
            
            // Set Options
            options = new object[idents.Count];
            for (int i = 0; i < idents.Count; i++)
            {
                if (idents[i] == null)
                    continue;

                options[i] = idents[i];
            }

            return idents;
        }

        #endregion
    }
}