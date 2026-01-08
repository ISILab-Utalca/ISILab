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

        private static readonly Dictionary<string, string> ConnectionIconGuids =
           new()
           {
            { SchemaBehaviour.Connections[2],    "cd77d8067cf8b6b44ab23da9a62173c0" },
            { SchemaBehaviour.Connections[3],  "c0d00de1d82858c4b9d772a012caf67d" },
            { SchemaBehaviour.Connections[4],  "f79cf4ba5777aab4a884bca201ff0278" },
            { SchemaBehaviour.Connections[5], "8e1818e3f49414e4997ecc63e331999f" },
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

        private static VectorImage LoadIcon(string guid)
        {
            if (!IconCache.TryGetValue(guid, out var icon))
            {
                icon = AssetMacro.LoadAssetByGuid<VectorImage>(guid);
                IconCache[guid] = icon;
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

            if (!ConnectionIconGuids.TryGetValue(this.connectionType, out string guid))
            {
                style.display = DisplayStyle.None;
                return;
            }

            Icon.style.backgroundImage = new StyleBackground(LoadIcon(guid));

        }

    }
}