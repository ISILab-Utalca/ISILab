using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.UI.Editor;
using LBS.VisualElements;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class AddonConnectionView : LBSCustomEditor
    {
        #region FIELDS
        [SerializeField]
        Addon_Unlock unlock;

        private string startingLabel;
        private VectorImage startingIcon;

        #endregion

        #region VIEW FIELDS
        LBSCustomImage connectionIcon;
        LBSCustomLabelIcon layerDisplay;
        LBSCustomButton clearRef;

        PickerConnect pickerConnect;

        SchemaTileConnectionView connectionTileView;
        static VisualTreeAsset visualTree { get; set; }

        #endregion

        #region Constructors
        public AddonConnectionView()
        {
            CreateVisualElement();
        }

        public AddonConnectionView(Addon_Unlock unlock)
        {
            CreateVisualElement();
            SetInfo(unlock);

        }
        #endregion

        #region METHODS

        public override void SetInfo(object paramTarget)
        {
            unlock = (Addon_Unlock)paramTarget;
            UpdateConnection();

        }

        protected override VisualElement CreateVisualElement()
        {
            if (visualTree is null)
            {
                visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("AddonConnectionView", true);
            }

            visualTree.CloneTree(this);

            connectionIcon = this.Q<LBSCustomImage>("ConnectionIcon");
            layerDisplay = this.Q<LBSCustomLabelIcon>("LayerDisplay");
            pickerConnect = this.Q<PickerConnect>("PickerConnect");
            clearRef = this.Q<LBSCustomButton>("ClearRef");

            clearRef.clicked += Reset;

            pickerConnect.OnConnectionClicked += OnChange;
            
            this.RegisterCallback<MouseEnterEvent>(_ =>
            {
                HighlightConnection();
            });

            this.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button != 0) return;
                HighlightConnection();
            });

            startingLabel = layerDisplay.Label;
            startingIcon = connectionIcon.LBSImage;
            return this;

        }

        private void Reset()
        {
            connectionIcon.LBSImage = startingIcon;
            layerDisplay.Label = startingLabel;
            connectionTileView = null;

            // Set new empty value
            if (unlock is not null) unlock.Connection = new ConnectionData();
         
        }

        private void OnChange(SchemaTileConnectionView schemaTile, ConnectionData connection)
        {
            if (unlock is null) return;

            object mani = ToolKit.Instance.GetActiveManipulator();
            var picker = ToolKit.Instance.GetTool(typeof(ConnectionPicker));

            // only update changed data on the connection view that activated the manipulator
            if (mani is ConnectionPicker cpicker)
            {
                if(pickerConnect == cpicker.Activator)
                {
                    connectionTileView = schemaTile;

                    // no reconnect same
                    if (unlock.Connection is not null && connection is not null &&
                        unlock.Connection.Equals(connection)) return;

                    unlock.Connection = connection;
                    LBSFocusHighlight.Highlight(this);
                    UpdateConnection();
                }

            }
        }

        private void UpdateConnection()
        {
            if (unlock is null)
            {
                Reset();
                return;
            }

            var connection = unlock.Connection;
            if (connection is null) return;

            if (connection.connections.Any())
            {
                connection.connections.ForEach(conn =>
                {
                    connectionIcon.LBSImage = SchemaTileConnectionView.GetIcon(conn.connection);
                });
            }

            if(connection.layer is not null)
            {
                layerDisplay.Label = connection.layer.Name;
                List<GraphElement> tiles = MainView.Instance.GetElementsFromLayer(connection.layer, connection.tile);
                if (tiles is not null)
                {
                    foreach (GraphElement tile in tiles)
                    {
                        SchemaTileConnectionView sctcv = tile.Q<SchemaTileConnectionView>();
                        if (sctcv is not null) connectionTileView = sctcv;
                    }
                }
            }

            HighlightConnection();
        }

        private void HighlightConnection()
        {
            if (connectionTileView == null) return;
            LBSFocusHighlight.Highlight(connectionTileView);
        }


        #endregion
    }
}