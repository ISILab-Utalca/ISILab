using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using LBS.Components;
using LBS.VisualElements;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class SchemaTileConnectionView : GraphElement
    {
        #region VIEW FIELDS

        private VisualElement Icon;
        private LBSLayer _ownerLayer;
        private LBSTile tile;

        #endregion

        #region STATIC FIELDS

        public static SchemaTileConnectionView SelectedTile;
        private static VisualTreeAsset view;

        public static readonly Dictionary<string, string> ConnectionIconGuids =
           new()
           {
               { SchemaBehaviour.Door,    "cd77d8067cf8b6b44ab23da9a62173c0" },
               { SchemaBehaviour.Window,  "c0d00de1d82858c4b9d772a012caf67d" },
               { SchemaBehaviour.LockedDoor,  "f79cf4ba5777aab4a884bca201ff0278" },
               { SchemaBehaviour.StairsUp, "76bf813a38668ce439887addd209058c" },
               { SchemaBehaviour.StairsDown, "76bf813a38668ce439887addd209058c" }
            //   ,{ SchemaBehaviour.BlockedDoor, "8e1818e3f49414e4997ecc63e331999f" },
           };

        private static readonly Dictionary<string, VectorImage> IconCache = new();
        #endregion

        #region FIELDS
        private string connectionType;
        private string connectionSide;
        
        private Color normalColor;
        // Set outline width here NOT in uxml
        private float normalBorder = 0f;
        private float hoverIncrease = 0f;
        #endregion

        #region PROPERTIES
        public string Type { get => connectionType; }
        public string Direction { get => connectionSide; }
        public LBSTile Tile { get => tile; }
        public LBSLayer Layer { get => _ownerLayer; }
        public static VectorImage GetIcon(string connection)
        {
            if (!IconCache.TryGetValue(connection, out VectorImage icon))
            {
                string guid = "dc346d42af837b04a985ee71996040db";
                if (ConnectionIconGuids.ContainsKey(connection)) guid = ConnectionIconGuids[connection];

                icon = AssetMacro.LoadAssetByGuid<VectorImage>(guid);
                IconCache[connection] = icon;
            }
            return icon;
        }
        #endregion

        public SchemaTileConnectionView(LBSLayer layer, LBSTile tile, string connectionType, string connectionSide)
        {
            if (view == null)
            {
                view = DirectoryTools.GetAssetByName<VisualTreeAsset>("SchemaTileConnectionView");
            }
            view.CloneTree(this);

            Icon = this.Q<VisualElement>("Icon");
            List<string> connectionTypes = SchemaBehaviour.Connections;

            this._ownerLayer = layer;
            this.tile = tile;
            this.connectionType = connectionType;
            this.connectionSide = connectionSide;

            VectorImage connectionIcon = GetIcon(this.connectionType);
            if (Icon is null)
            {
                style.display = DisplayStyle.None;
                return;
            }

            Icon.style.backgroundImage = new StyleBackground(connectionIcon);

            /*
            RegisterCallback<PointerEnterEvent>(OnHover);
            RegisterCallback<PointerLeaveEvent>(OnUnhover);
            RegisterCallback<MouseDownEvent>(OnMouseDown);//*/
        }

        private void OnHover(PointerEnterEvent evt)
        {
            // Color change
            normalColor = style.backgroundColor.value;
            var _hoverColor = normalColor;
            _hoverColor.a = 1f;
            _hoverColor.r *= hoverIncrease;
            _hoverColor.g *= hoverIncrease;
            _hoverColor.b *= hoverIncrease;
            style.backgroundColor = _hoverColor;
        }

        private void OnUnhover(PointerLeaveEvent evt)
        {
            style.backgroundColor = normalColor;
        }
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (LBSMainWindow.Instance._selectedLayer != _ownerLayer) return;
            if (ToolKit.Instance.GetActiveManipulator() is null) return;
            if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator))
            {
                //_tileBehaviour.SelectedTilemap = _tileBundleGroup;
                LBSInspectorPanel.ActivateBehaviourTab();

                SelectedTile = this;
            }
        }
    }
}