using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.Extensions;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using LBS.Components;
using LBS.VisualElements;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class StairsGraph : GraphElement
    {
        private readonly SchemaBehaviour _schemaBehaviour;
        private readonly VectorImage _upIcon = 
            AssetMacro.LoadAssetByGuid<VectorImage>("103cf2403fa02574fb824cdb84514eb9");
        private readonly VectorImage _downIcon = 
            AssetMacro.LoadAssetByGuid<VectorImage>("07047a27e6d6d5b4a87df2580a6068f4");

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
            this.Add(_startTile);
            this.Add(_endTile);

            _stair = stair;
            _stair.OnVisualChange = Update;

            _ownerLayer = ownerLayer;
            this.generateVisualContent += DrawLine;


            _schemaBehaviour = ownerLayer.GetBehaviour<SchemaBehaviour>();


            SetVisualElements();
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (LBSMainWindow.Instance._selectedLayer != _ownerLayer) return;
            if (ToolKit.Instance.GetActiveManipulator() is null) return;
            if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator))
            {
                LBSInspectorPanel.ActivateDataTab();
            }

        }

        public void Update(LBSStair stair)
        {
            _stair = stair;
            SetVisualElements();
            MarkDirtyRepaint();
        }

        private void SetVisualElements()
        {
            // Set colors
            _startTile.SetColor(Color.black);
            _endTile.SetColor(Color.black);

            // Set icons
            bool upwards = _stair.Direction > 0;
            if (_ownerLayer.ActiveFloor == _stair.InferiorFloor)
            {
                _startTile.SetImage(upwards ? _downIcon : _upIcon);
                _endTile.SetImage(upwards ? _upIcon : _downIcon);
            }
            else if(_ownerLayer.ActiveFloor == _stair.SuperiorFloor)
            {
                _startTile.SetImage(upwards ? _upIcon : _downIcon);
                _endTile.SetImage(upwards ? _downIcon : _upIcon);
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
            painter.DrawPolygon(fixedPositions, new Color(0, 0, 0, 0), _stair.Color, 4, false);
            painter.DrawCircle(fixedPositions[0], 12, _stair.Color);
            painter.DrawCircle(fixedPositions[line.Count - 1], 12, _stair.Color);
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

            var firstDir = output[1] - output[0];
            var lastDir = output[output.Count - 1] - output[output.Count - 2];
            output[0] += firstDir * 0.5f;
            output[output.Count - 1] -= lastDir * 0.5f;
            return output;
        }
    }
}
