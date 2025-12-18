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

        }

        protected override void OnKeyDown(KeyDownEvent e)
        {
            base.OnKeyDown(e);
            MainView.Instance.RemoveElement(_iconFeedback);
        }

        protected override void OnMouseLeave(VisualElement element, MouseLeaveEvent e)
        {
            MainView.Instance.RemoveElement(_dottedFeedback);
            MainView.Instance.RemoveElement(_iconFeedback);
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            base.OnMouseUp(element, endPosition, e);

            MainView.Instance.RemoveElement(_iconFeedback);

            //If esc key was pressed, cancel the operation
            if (ForceCancel)
            {
                MainView.Instance.RemoveElement(_dottedFeedback);
                ForceCancel = false;
                return;
            }

            var endPos = _population.OwnerLayer.ToFixedPosition(endPosition);

            // Check if the move is valid
            if (!_population.BundleTilemap.ValidMoveGroup(endPos, Selected, Vector2.right)) return;
               

            // Calculate the difference between the new position and the original top-left position of the group
            Vector2Int originalTopLeft = Selected.TileGroup[0].Position;
            Vector2Int offset = endPos - originalTopLeft;

            // Move each tile relative to the offset
            //Selected.Translate(offset);
            _population.MoveGroup(Selected, offset);

            _population.OwnerLayer.OnChangeUpdate();
            DrawManager.Instance.RedrawLayer(_population.OwnerLayer);
        }

        protected override void OnMouseDown(VisualElement element, Vector2Int startPosition, MouseDownEvent e)
        { 
            var position = _population.OwnerLayer.ToFixedPosition(startPosition);
            var tileGroup = _population.GetTileGroup(position);
            if (tileGroup == null ||
                tileGroup.BundleData == null ||
                !tileGroup.BundleData.Bundle)
            {
                Selected = null;
                return;
            }
             
            Selected = tileGroup;
            _iconFeedback.Icon = Selected.BundleData.Bundle.Icon;
            MainView.Instance.AddElement(_iconFeedback);
        }
        
        // TODO Currently it completely bugs out whenever x or y are 0 in the grid space. why? wish i fucking knew
        protected override void OnMouseMove(VisualElement element, Vector2Int movePosition, MouseMoveEvent e)
        {
            MainView.Instance.RemoveElement(_dottedFeedback);

            if (ForceCancel) return;
 
            var topLeftCorner = -_population.OwnerLayer.ToFixedPosition(movePosition); // use negative value for corner
            var bottomRightCorner = topLeftCorner;

            // Set corner by tile size
            if (ToSet.TileSize.x > 1 || ToSet.TileSize.y > 1 )
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
            if(movePosition.y < 0)
            {
                firstPos.y += 99;
                lastPos.y += 99;
            }
            if(movePosition.x < 0)
            {
                firstPos.x -= 99;
                lastPos.x -= 99;
            }
            firstPos.x *= -1;
            lastPos.x *= -1;
            
            _dottedFeedback.UpdatePositions(firstPos.ToInt(), lastPos.ToInt());
            MainView.Instance.AddElement(_dottedFeedback);


            bool valid;
            // dragging feedback
            if (Selected != null)
            {
                // undo the negative of topLeftCorner
                valid = _population.ValidMoveGroup(-topLeftCorner, Selected); 
                _dottedFeedback.ValidForInput(valid);
                _iconFeedback.UpdatePositions(firstPos.ToInt(), lastPos.ToInt());
            }
            // adding feedback
            else
            {
                var position = _population.OwnerLayer.ToFixedPosition(movePosition);
                var tileGroup = _population.GetTileGroup(position);
                if (tileGroup == null ||
                    tileGroup.BundleData == null ||
                    !tileGroup.BundleData.Bundle)
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