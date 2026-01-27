using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.UI.Editor;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using LBS.Components;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Manipulators
{
    public class MoveSchemaZone : LBSManipulator
    {
        private SchemaBehaviour _schema;
        private Zone _selectedZone;
        private Vector2Int _startDragPos;
        private Vector2Int _zoneMinOffset;
        private Vector2Int _zoneMaxOffset;

        private List<LBSTile> _cachedZoneTiles;

        private readonly Feedback _dottedFeedback = new DottedAreaFeedback();

        protected override string IconGuid => "ad3a2ec3b8f589d42a66626c44a3fd17";

        public MoveSchemaZone()
        {
            Name = "Move Zone";
            Description = "Drag a zone to move the entire room and its connections.";

            _dottedFeedback.preview = true;
            _dottedFeedback.fixToTeselation = true;
        }

        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
            _schema = provider as SchemaBehaviour;
            _schema.OwnerLayer.OnChange += CleanUpVisuals;
        }

        protected override void OnMouseLeave(VisualElement element, MouseLeaveEvent e)
        {
            CleanUpVisuals();

        }

        protected override void OnMouseEnter(VisualElement element, MouseEnterEvent e)
        {
            CleanUpVisuals();
        }

        protected override void OnMouseDown(VisualElement element, Vector2Int startPosition, MouseDownEvent e)
        {
            if (e.button != 0) return;

            var pos = _schema.OwnerLayer.ToFixedPosition(startPosition);
            var tile = _schema.GetTile(pos);

            if (tile == null)
            {
                _selectedZone = null;
                return;
            }

            _selectedZone = _schema.GetZone(tile);
            _startDragPos = pos;

            _cachedZoneTiles = _schema.GetTiles(_selectedZone);

            CalculateZoneBounds(_cachedZoneTiles, pos);

            ISILab.LBS.Plugin.UI.Editor.MainView.Instance.AddElement(_dottedFeedback);
            ActualizeFeedback(pos);
        }

        protected override void OnMouseMove(VisualElement element, Vector2Int movePosition, MouseMoveEvent e)
        {
            if (_selectedZone == null)
            {
                CleanUpVisuals();
                return;
            }

            if (ForceCancel)
            {
                CleanUpVisuals();
                return;
            }

            base.OnMouseMove(element, movePosition, e);

            var currentGridPos = _schema.OwnerLayer.ToFixedPosition(movePosition);
            ActualizeFeedback(currentGridPos);
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            if (_selectedZone == null || e.button != 0) return;

            if (ForceCancel)
            {
                ForceCancel = false;
                CleanUp();
                return;
            }

            var finalPos = _schema.OwnerLayer.ToFixedPosition(endPosition);
            Vector2Int offset = finalPos - _startDragPos;

            if (offset == Vector2Int.zero)
            {
                CleanUp();
                return;
            }

            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(LBSController.CurrentLevel, "Move Zone");

            bool success = _schema.MoveZone(_selectedZone, offset);

            if (!success)
            {
                Debug.LogWarning("Cannot move zone here due to collision.");
            }
            else
            {
                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(LBSController.CurrentLevel);
            }

            CleanUp();
        }

        private void CleanUpVisuals()
        {
            if (ISILab.LBS.Plugin.UI.Editor.MainView.Instance != null)
            {
                ISILab.LBS.Plugin.UI.Editor.MainView.Instance.RemoveElement(_dottedFeedback);
            }
        }

        private void CleanUp()
        {
            _selectedZone = null;
            _cachedZoneTiles = null;
            CleanUpVisuals();
        }

        private void CalculateZoneBounds(List<LBSTile> tiles, Vector2Int anchorPos)
        {
            if (tiles == null || tiles.Count == 0) return;

            Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
            Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);

            foreach (var t in tiles)
            {
                min = Vector2Int.Min(min, t.Position);
                max = Vector2Int.Max(max, t.Position);
            }

            _zoneMinOffset = min - anchorPos;
            _zoneMaxOffset = max - anchorPos;
        }

        private void ActualizeFeedback(Vector2Int currentGridPos)
        {
            Vector2Int offset = currentGridPos - _startDragPos;
            bool isValid = CheckCollision(offset);

            _dottedFeedback.ValidForInput(isValid);

            Vector2Int targetMin = currentGridPos + _zoneMinOffset;
            Vector2Int targetMax = currentGridPos + _zoneMaxOffset;

            var topLeftCorner = -targetMin;
            var bottomRightCorner = -targetMax;

            var firstPos = _schema.OwnerLayer.FixedToPosition(topLeftCorner);
            var lastPos = _schema.OwnerLayer.FixedToPosition(bottomRightCorner);

            firstPos.y += 99;
            lastPos.y += 99;

            if (targetMin.x < 0) firstPos.x -= 99;
            if (targetMax.x < 0) lastPos.x -= 99;

            firstPos.x *= -1;
            lastPos.x *= -1;

            _dottedFeedback.UpdatePositions(new Vector2Int((int)firstPos.x, (int)firstPos.y), new Vector2Int((int)lastPos.x, (int)lastPos.y));
        }

        private bool CheckCollision(Vector2Int offset)
        {
            if (offset == Vector2Int.zero) return true;
            if (_cachedZoneTiles == null) return false;

            foreach (var tile in _cachedZoneTiles)
            {
                Vector2Int targetPos = tile.Position + offset;
                var existingTile = _schema.GetTile(targetPos);

                if (existingTile != null)
                {
                    var existingZone = _schema.GetZone(existingTile);
                    if (existingZone != _selectedZone)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}