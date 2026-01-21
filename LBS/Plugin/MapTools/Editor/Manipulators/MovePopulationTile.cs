using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.VisualElements;
using LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.VisualElements.Editor;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;
using ISILab.LBS.Editor.Windows;
using UnityEditor;

namespace ISILab.LBS.Manipulators
{
    public class MovePopulationTile : LBSManipulator
    {
        private PopulationBehaviour _population;

        private readonly Feedback _dottedFeedback;
        private readonly IconFeedback _iconFeedback;
        // Used to access from me draw manager
        public TileBundleGroup Selected { get; private set; }
        
        protected override string IconGuid => "ad3a2ec3b8f589d42a66626c44a3fd17";

        private Bundle ToSet => _population.selectedToSet;

        public MovePopulationTile()
        {
            _dottedFeedback = new DottedAreaFeedback();
            _dottedFeedback.preview = true;
            _dottedFeedback.fixToTeselation = true;

            _iconFeedback = new IconFeedback();
            _iconFeedback.preview = true;
            _iconFeedback.fixToTeselation = true;

            Name = "Move Item Tile";
            Description =
                "Click on the graph to and drag a population tile to move it.";
        }

        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
            _population = provider as PopulationBehaviour;

            _population.OwnerLayer.OnChange += () =>
            {
                MainView.Instance.RemoveElement(_dottedFeedback);
                MainView.Instance.RemoveElement(_iconFeedback);
            };

            //OnManipulationRightClick += () =>
            //{
            //    ForceCancel = true;
            //    OnToolUsage = false;

            //    LBSMainWindow.MessageNotify("'" + Name + "' action cancelled.");
            //    Feedback?.SetDisplay(false);
            //    MainView.Instance.RemoveElement(_iconFeedback);
            //    MainView.Instance.AddElement(_dottedFeedback);
            //    _dottedFeedback.ValidForInput(false);
            //    Selected = null;
            //};
        }

        //protected override void OnKeyDown(KeyDownEvent e)
        //{
        //    base.OnKeyDown(e);
        //    MainView.Instance.RemoveElement(_iconFeedback);
        //}

        protected override void OnMouseLeave(VisualElement element, MouseLeaveEvent e)
        {
            if (Selected == null)
            {
                MainView.Instance.RemoveElement(_dottedFeedback);
                MainView.Instance.RemoveElement(_iconFeedback);
            }
        }

        protected override void OnMouseDown(VisualElement element, Vector2Int startPosition, MouseDownEvent e)
        {
            if (e.button == 0)
            {
                var position = _population.OwnerLayer.ToFixedPosition(startPosition);
                var tileGroup = _population.GetTileGroup(position);
                if (tileGroup == null || tileGroup.BundleData == null || !tileGroup.BundleData.Bundle)
                {
                    Selected = null;
                    return;
                }

                Selected = tileGroup;
                _iconFeedback.Icon = Selected.BundleData.Bundle.Icon;
                MainView.Instance.AddElement(_iconFeedback);
                ActualizeFeedbackPosition(startPosition);
            }
        }

        protected override void OnMouseMove(VisualElement element, Vector2Int movePosition, MouseMoveEvent e)
        {
            MainView.Instance.RemoveElement(_dottedFeedback);

            if (ForceCancel)
            {
                MainView.Instance.RemoveElement(_iconFeedback);
                _dottedFeedback.ValidForInput(false);
                return;
            }

            ActualizeFeedbackPosition(movePosition);
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            if (e.button == 0)
            {
                base.OnMouseUp(element, endPosition, e);

                MainView.Instance.RemoveElement(_iconFeedback);

                //If esc key was pressed, cancel the operation
                if (ForceCancel)
                {
                    _dottedFeedback.ValidForInput(false);
                    ForceCancel = false;
                    return;
                }

                if (Selected == null) return;

                var endPos = _population.OwnerLayer.ToFixedPosition(endPosition);

                // Check if the move is valid
                if (!_population.BundleTilemap.ValidMoveGroup(endPos, Selected, Vector2.right)) return;

                var level = LBSController.CurrentLevel;
                EditorGUI.BeginChangeCheck();
                Undo.RegisterCompleteObjectUndo(level, "Move Element population");

                // Calculate the difference between the new position and the original top-left position of the group
                Vector2Int originalTopLeft = Selected.TileGroup[0].Position;
                Vector2Int offset = endPos - originalTopLeft;

                // Move each tile relative to the offset
                //Selected.Translate(offset);
                _population.MoveGroup(Selected, offset);

                _population.OwnerLayer.OnChangeUpdate();
                DrawManager.Instance.DrawSingleComponent(_population, _population.OwnerLayer);
                //DrawManager.Instance.RedrawLayer(_population.OwnerLayer);

                _dottedFeedback.ValidForInput(false);
                Selected = null;

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(level);
                }
            }
        }

        protected override void OnKeyDown(KeyDownEvent e)
        {
            base.OnKeyDown(e);

            if ((e.keyCode == KeyCode.Escape) && ForceCancel)
            {
                MainView.Instance.RemoveElement(_iconFeedback);
                _dottedFeedback.ValidForInput(false);
                MainView.Instance.AddElement(_dottedFeedback);
                Selected = null;
            }
        }

        private void ActualizeFeedbackPosition(Vector2 pos)
        {
            var topLeftCorner = -_population.OwnerLayer.ToFixedPosition(pos); // use negative value for corner
            var bottomRightCorner = topLeftCorner;

            // Set corner by tile size
            if (ToSet.TileSize.x > 1 || ToSet.TileSize.y > 1)
            {
                var offset = ToSet.TileSize - new Vector2Int(1, 1);
                offset.x = -Mathf.Abs(offset.x);
                offset.y = Mathf.Abs(offset.y);
                bottomRightCorner += offset;
            }

            // grid to local position
            var firstPos = _population.OwnerLayer.FixedToPosition(topLeftCorner);
            var lastPos = _population.OwnerLayer.FixedToPosition(bottomRightCorner);

            // weird correction on coordinates, hate it but it works
            if (pos.y < 0)
            {
                firstPos.y += 99;
                lastPos.y += 99;
            }
            if (pos.x < 0)
            {
                firstPos.x -= 99;
                lastPos.x -= 99;
            }
            firstPos.x *= -1;
            lastPos.x *= -1;

            _dottedFeedback.UpdatePositions(firstPos.ToInt(), lastPos.ToInt());
            MainView.Instance.AddElement(_dottedFeedback);


            // dragging feedback
            if (Selected != null)
            {
                // undo the negative of topLeftCorner
                bool valid = _population.BundleTilemap.ValidMoveGroup(-topLeftCorner, Selected, Vector2.right);
                _dottedFeedback.ValidForInput(valid);
                _iconFeedback.UpdatePositions(firstPos.ToInt(), lastPos.ToInt());
            }
            // adding feedback
            else
            {
                var position = _population.OwnerLayer.ToFixedPosition(pos);
                var tileGroup = _population.GetTileGroup(position);
                if (tileGroup == null || tileGroup.BundleData == null || !tileGroup.BundleData.Bundle)
                {
                    _dottedFeedback.ValidForInput(false);
                    return;
                }

                // undo the negative of topLeftCorner
                _dottedFeedback.ValidForInput(true);
            }
        }
    }
}