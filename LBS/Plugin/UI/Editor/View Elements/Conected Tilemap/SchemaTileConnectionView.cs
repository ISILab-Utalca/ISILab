using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class SchemaTileConnectionView : GraphElement
    {
        #region VIEW FIELDS

        private VisualElement Icon;
        private LBSTile tile;

        #endregion

        #region STATIC FIELDS

        private static VisualTreeAsset view;

        public static readonly Dictionary<string, string> ConnectionIconGuids =
           new()
           {
            { SchemaBehaviour.Door,    "cd77d8067cf8b6b44ab23da9a62173c0" },
            { SchemaBehaviour.Window,  "c0d00de1d82858c4b9d772a012caf67d" },
            { SchemaBehaviour.LockedDoor,  "f79cf4ba5777aab4a884bca201ff0278" },
            { SchemaBehaviour.BlockedDoor, "8e1818e3f49414e4997ecc63e331999f" },
           };

        private static readonly Dictionary<string, VectorImage> IconCache = new();
        #endregion

        #region FIELDS
        private string connectionType;
        private string connectionSide;
        #endregion

        #region PROPERTIES
        public string Type { get => connectionType; }
        public string Direction { get => connectionSide; }
        public LBSTile Tile { get => tile; }

        public static VectorImage GetIcon(string connection)
        {
            if (!IconCache.TryGetValue(connection, out VectorImage icon))
            {
                icon = AssetMacro.LoadAssetByGuid<VectorImage>(ConnectionIconGuids[connection]);
                IconCache[connection] = icon;
            }
            return icon;
        }
        #endregion

        public SchemaTileConnectionView(LBSTile tile, string connectionType, string connectionSide)
        {
            if (view == null)
            {
                view = DirectoryTools.GetAssetByName<VisualTreeAsset>("SchemaTileConnectionView");
            }
            view.CloneTree(this);

            Icon = this.Q<VisualElement>("Icon");
            List<string> connectionTypes = SchemaBehaviour.Connections;

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

        }

    }
}