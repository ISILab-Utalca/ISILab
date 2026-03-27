using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.Extensions;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using LBS.Components;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static PathOS.PathOSNavUtility.NavmeshMemoryMapper;
using static UnityEditor.PlayerSettings;

namespace ISILab.LBS.VisualElements
{
    public class StairsGraph : GraphElement
    {
        private readonly VectorImage _upIcon = 
            AssetMacro.LoadAssetByGuid<VectorImage>("103cf2403fa02574fb824cdb84514eb9");
        private readonly VectorImage _downIcon = 
            AssetMacro.LoadAssetByGuid<VectorImage>("103cf2403fa02574fb824cdb84514eb9");

        private LBSStair _stair;
        private LBSLayer _ownerLayer;

        private static VisualTreeAsset stairsTree;
        private static VisualTreeAsset popTree;

        private PopulationTileView _startTile;
        private PopulationTileView _endTile;

        public StairsGraph(LBSStair stair, LBSLayer ownerLayer)
        {
            if(stairsTree is null) stairsTree = 
                    ScriptableObject.CreateInstance("VisualTreeAsset") as VisualTreeAsset;
            stairsTree.CloneTree(this);

            if (popTree == null)
            {
                popTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("PopulationTile", true);
            }
            _startTile = new (null, ownerLayer);
            _endTile = new (null, ownerLayer);
            popTree.CloneTree(_startTile);
            popTree.CloneTree(_endTile);
            this.Add(_startTile);
            this.Add(_endTile);

            _stair = stair;
            _ownerLayer = ownerLayer;
            this.generateVisualContent += DrawLine;

            SetVisualElements();
        }

        public void Update(LBSStair stair)
        {
            _stair = stair;
            SetVisualElements();
        }

        private void SetVisualElements()
        {
            // Set icons
            if (_ownerLayer.ActiveFloor == _stair.InferiorFloor)
            {
                _startTile.SetImage(_upIcon);
                _endTile.SetImage(_downIcon);
            }
            else if(_ownerLayer.ActiveFloor == _stair.SuperiorFloor)
            {
                _startTile.SetImage(_downIcon);
                _endTile.SetImage(_upIcon);
            }

            // Hide arrows
            _startTile.HideArrows();
            _endTile.HideArrows();

            // Set positions
            _startTile.SetPosition(new Rect(Vector2.one * 50, Vector2.zero));
            var relativePos = _stair.Positions[_stair.Positions.Count - 1] - _stair.Positions[0];
            relativePos *= new Vector2Int(1, -1);
            _endTile.SetPosition(new Rect(Vector2.one * 50 + relativePos * 100, Vector2.zero));
        }

        private void DrawLine(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;

            var line = _stair.Positions;
            /*line.RemoveAt(0);
            line.RemoveAt(line.Count - 1);
            if (line.Count == 0) return;//*/

            var fixedPositions = GetFixedPositions(line);
            painter.DrawPolygon(fixedPositions, new Color(0, 0, 0, 0), Color.white, 4, false);
            painter.DrawCircle(fixedPositions[0], 16, Color.white);
            painter.DrawCircle(fixedPositions[line.Count - 1], 16, Color.white);
        }
        private List<Vector2> GetFixedPositions(List<Vector2Int> line)
        {
            List<Vector2> output = new();
            Vector2Int? origin = null;
            foreach (var pos in line)
            {
                var v = pos * 100; 
                if (origin is null) origin = v;
                v = (v - origin.Value) * new Vector2Int(1, -1) + new Vector2Int(50, 50);
                output.Add(v);
            }
            return output;
        }
    }
}
