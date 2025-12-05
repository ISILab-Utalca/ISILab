using ISILab.Commons;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Modules;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Editor.Windows;
using LBS.Components;
using ISILab.LBS.Behaviours;
using UnityEditor.Experimental.GraphView;
using LBS.VisualElements;
using ISILab.LBS.Manipulators;

namespace ISILab.LBS.VisualElements
{
    public class PopulationTileView : GraphElement
    {
        #region CONSTANTS
        public static PopulationTileView SelectedTile;
        private const float defaultAlpha = 0.75f;
        #endregion

        #region STATIC
        private static VisualTreeAsset view;
        #endregion

        #region FIELDS
        private TileBundleGroup _tileBundleGroup;
        private readonly LBSLayer _ownerLayer;
        private readonly TileGroupBehavior _tileBehaviour;

        private List<VisualElement> _arrows = new();
        private VisualElement _pivot;
        private VisualElement _icon;
        private VisualElement _background;
        private StyleColor _borderColor;
        private StyleColor _backgroundColor;


        #endregion

        #region CONSTRUCTOR
        public PopulationTileView(TileBundleGroup tile, LBSLayer ownerLayer)
        {
            LoadVisualElement();
 
            _tileBundleGroup = tile;
            _ownerLayer = ownerLayer;
            _tileBehaviour = ownerLayer.GetBehaviour<TileGroupBehavior>();
            _tileBehaviour.OnSelectedChanged += (newTbg)=> Highlight(newTbg == _tileBundleGroup);
            InitializeVisuals(tile);
            SetupCallbacks();


            Highlight(_tileBundleGroup==_tileBehaviour.SelectedTilemap);
        }
        #endregion

        #region INITIALIZATION
        private void LoadVisualElement()
        {
            //    if (view == null)
            view = DirectoryTools.GetAssetByName<VisualTreeAsset>("PopulationTile", true);

            view.CloneTree(this);

            _arrows.Add(this.Q<VisualElement>("Up"));
            _arrows.Add(this.Q<VisualElement>("Left"));
            _arrows.Add(this.Q<VisualElement>("Down"));
            _arrows.Add(this.Q<VisualElement>("Right"));

            _pivot = this.Q<VisualElement>("Pivot");
            _icon = this.Q<VisualElement>("Icon");
            _background = this.Q<VisualElement>("Background");

            _borderColor = _background.style.borderBottomColor;
            
        }

        private void InitializeVisuals(TileBundleGroup tile)
        {
            Bundle bundle = tile.BundleData.Bundle;
            _backgroundColor = bundle.Color;
            SetColor(bundle.Color);
            SetImage(bundle.Icon);
            SetDirection(tile.Rotation);

            if (bundle.GetHasTagCharacteristic("NonRotate"))
                HideArrows();
        }

        private void SetupCallbacks()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }
        #endregion

        #region EVENT HANDLERS
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (LBSMainWindow.Instance._selectedLayer != _ownerLayer) return;

            if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator))
            {                
                _tileBehaviour.SelectedTilemap = _tileBundleGroup;
                LBSInspectorPanel.ActivateBehaviourTab();

                SelectedTile?.Highlight(false);
                SelectedTile = this;
                Highlight(true);
            }
  
        }
        #endregion

        #region VISUAL CONTROL
        public void SetDirection(Vector2 direction)
        {
            int dirIndex = Directions.Bidimencional.Edges
                .Select((d, i) => new { d, i })
                .OrderBy(o => (direction - o.d).magnitude)
                .First().i;

            _arrows.ForEach(a => a.visible = false);
            _arrows[dirIndex].visible = true;
        }

        public void HideArrows()
        {
            foreach (var arrow in _arrows)
                arrow.style.display = DisplayStyle.None;
        }

        public void SetPivot(Vector2 pivot)
        {
            _pivot.style.left = pivot.x;
            _pivot.style.top = pivot.y;
        }

        public void SetSize(Vector2 size)
        {
            _pivot.style.width = size.x;
            _pivot.style.height = size.y;
        }

        public void SetColor(Color color)
        {
            _background.style.backgroundColor = color;
        }

        public void SetImage(VectorImage icon)
        {
            _icon.style.backgroundImage = new StyleBackground(icon);
        }

        public void Highlight(bool highlight)
        {
            if (LBSMainWindow.Instance._selectedLayer != _ownerLayer) return;

            float backgroundAlpha = highlight ? 1f : defaultAlpha;
            var newColor = (new Color(_backgroundColor.value.r, _backgroundColor.value.g, _backgroundColor.value.b, backgroundAlpha));

            SetColor(newColor);

            var defaultBorder = Color.black;
            defaultBorder.a = 0.33f;
            StyleColor newValue = highlight ? new StyleColor(Color.white) : new StyleColor(defaultBorder);
            _background.style.borderRightColor = newValue;
            _background.style.borderBottomColor = newValue;
            _background.style.borderLeftColor = newValue;
            _background.style.borderTopColor = newValue;

            if (highlight) _pivot.AddToClassList("selected");
            else _pivot.RemoveFromClassList("selected");

        }
        #endregion
    }
}
