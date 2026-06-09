using LBS.Components;
using ISILab.Commons.Extensions;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Manipulators
{
    public class AddVertexExteriorTile : ManipulateTeselation
    {
        //private static List<Vector2Int> Directions => Commons.Directions.Bidimencional.Edges;
        private List<Vector2Int> NeighbourDirections = Commons.Directions.Bidimencional.All;
        private ExteriorBehaviour _exterior;
        protected override string IconGuid => "ce4ce3091e6cf864cbbdc1494feb6529";

        public AddVertexExteriorTile()
        {
            Name = "Add Tile";
            Description = "Add an Exterior Tile. Hold CTRL to keep neighbors intact.";
        }

        public override void Init(LBSLayer layer, object provider)
        {
            base.Init(layer, provider);

            _exterior = provider as ExteriorBehaviour;
        }

        protected override void OnKeyDown(KeyDownEvent e)
        {
            base.OnKeyDown(e);
            if (e.ctrlKey) LBSMainWindow.WarningManipulator("(CTRL) Preserving neighbour connections");
        }

        protected override void OnKeyUp(KeyUpEvent e)
        {
            base.OnKeyUp(e);
            LBSMainWindow.WarningManipulator();
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            base.OnMouseUp(element, endPosition, e);

            //If esc key was pressed, cancel the operation
            if (ForceCancel)
            {
                ForceCancel = false;
                return;
            }

            var x = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(x, "Add Exterior Tile");

            var paintNeighbors = !e.ctrlKey;

            var corners = _exterior.OwnerLayer.ToFixedPosition(StartPosition, EndPosition);

            for (int i = corners.Item1.x; i <= corners.Item2.x; i++)
            {
                for (int j = corners.Item1.y; j <= corners.Item2.y; j++)
                {
                    var pos = new Vector2Int(i, j);
                    var tile = new LBSTile(pos);

                    _exterior.AddTile(tile);


                    if (!_exterior.identifierToSet ||
                        _exterior.identifierToSet.Label == null /*|| _exterior.identifierToSet.Label == "Empty"*/) continue;

                    //if (_exterior.identifierToSet.Label == "Empty") paintNeighbors = false;

                    SetConnections(tile, pos, paintNeighbors, _exterior.identifierToSet.Label == "Empty");
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(x);
            }
        }

        private void SetConnections(LBSTile tile, Vector2Int pos, bool paintNeighbors, bool empty)
        {
            // Paint all connections
            for (int i = 0; i < 4; i++)
            {
                Vector2Int edgeNeighbour = NeighbourDirections[i * 2];
                Vector2Int vertexNeighbour = NeighbourDirections[i * 2 + 1];

                Vector2Int lowNeighbour = edgeNeighbour;
                Vector2Int midNeighbour = vertexNeighbour;
                Vector2Int highNeighbour = NeighbourDirections[(i + 1) * 2 % 8];

                LBSTile neighbour = _exterior.GetTile(pos + edgeNeighbour);

                LBSTile lowNeigh = neighbour;
                LBSTile midNeigh = _exterior.GetTile(pos + midNeighbour);
                LBSTile highNeigh = _exterior.GetTile(pos + highNeighbour);
                List<LBSTile> neighs = new() { lowNeigh, midNeigh, highNeigh };

                if (!paintNeighbors && !empty && neighs.RemoveEmpties().Count > 0)
                {
                    // Conservar conexiones de los vecinos
                    //string conn = _exterior.GetConnections(neighbour)[(i + 1) % 4];
                    List<string> conns = new();
                    for(int j = 0; j < 3; j++)
                    {
                        List<string> neighConns = _exterior.GetConnections(neighs[j]);
                        conns.Add(neighConns.Count > 0 ? neighConns[(i + j + 1) % 4] : "");
                    }
                    //{
                    //    _exterior.GetConnections(lowNeigh)[(i + 1) % 4],
                    //    _exterior.GetConnections(midNeigh)[(i + 2) % 4],
                    //    _exterior.GetConnections(highNeigh)[(i + 3) % 4]
                    //};
                    if (conns.ContainsOnly("", "Empty"))
                    {
                        _exterior.SetConnection(tile, i, _exterior.identifierToSet.Label, true);
                    }
                    else
                    {
                        _exterior.SetConnection(tile, i, conns.FirstOrDefault(c => c != "" && c != "Empty"), true);
                    }
                    continue;
                }

                _exterior.SetConnection(tile, i, _exterior.identifierToSet.Label, true);

                if (empty) continue;

                foreach (Vector2Int neighbourDir in new[] { edgeNeighbour, vertexNeighbour })
                {
                    neighbour = _exterior.GetTile(pos + neighbourDir);
                    if (neighbour is { })
                    {
                        List<int> indices = neighbourDir.GetVertices();
                        indices.ForEach(ind => _exterior.SetConnection(neighbour, ind, _exterior.identifierToSet.Label, true));
                    }
                }
            }
        }
    }
}