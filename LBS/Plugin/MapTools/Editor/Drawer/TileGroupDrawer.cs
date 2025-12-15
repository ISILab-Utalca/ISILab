using ISILab.Commons.VisualElements;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Drawers
{
    [Drawer(typeof(TileGroupBehavior))]
    public class PopulationTileDrawer : Drawer
    {
        private bool _subscribed;
        private TileGroupBehavior _tgb;

        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            if (target is not TileGroupBehavior tgb) return;
    
            if(_tgb is null)
            {
                _tgb = tgb;
                _tgb.OnSelectedChanged += _ =>
                {
                    PopulationTileGroupView.UpdateVisuals(null);
                    view.ClearLayerComponentView(_tgb.OwnerLayer, this);
                };
            }
            view.ClearLayerComponentView(_tgb.OwnerLayer, this);

            if (LBSMainWindow.Instance._selectedLayer != _tgb.OwnerLayer)
                return;

            var selected = _tgb.SelectedTilemap;
            if (selected == null) return;

            PopulationTileView.SelectedTile?.Highlight(true);
            DrawGroupView(view, selected);
            DrawPatrol(view, selected);
            DrawTriggers(view, selected);
        }

        private void DrawGroupView(MainView view, TileBundleGroup selected)
        {
            var groupView = new PopulationTileGroupView(selected);

            Vector2 tileSize = _tgb.OwnerLayer.TileSize * LBSSettings.Instance.general.TileSize;
            Vector2 pos = new Vector2(selected.GetBounds().x, -selected.GetBounds().y);

            groupView.SetPosition(new Rect(pos * tileSize, tileSize));
            groupView.layer = _tgb.OwnerLayer.index;

            view.AddElementToLayerContainer(_tgb.OwnerLayer, this, groupView);
        }

        private void DrawPatrol(MainView view, TileBundleGroup selected)
        {
            var pts = selected.Addons.patrol.Points;
            if (pts.Count <= 1) return;

            if (selected.Addons.patrol.Loop)
                DrawConnection(view, pts.Last(), pts.First());
            for (int i = 0; i < pts.Count - 1; i++)
                DrawConnection(view, pts[i], pts[i + 1], i);

        }

        private void DrawConnection(MainView view, Vector2 a, Vector2 b, int labelIndex = -1)
        {
            Vector2Int aCell = a.ToInt();
            Vector2Int bCell = b.ToInt();

            Vector2 aWorld = _tgb.OwnerLayer.FixedToPosition(aCell, true);
            Vector2 bWorld = _tgb.OwnerLayer.FixedToPosition(bCell, true);

            var feedback = new ConnectionFeedback();

            Vector2 tileSize = _tgb.OwnerLayer.TileSize * LBSSettings.Instance.general.TileSize;
            feedback.SetOffset(tileSize / 2);
            feedback.UpdatePositions(Color.white, aWorld.ToInt(), bWorld.ToInt());

            view.AddElementToLayerContainer(_tgb.OwnerLayer, this, feedback);

            if (labelIndex >= 0)
            {
                AddNumberLabel(labelIndex, aWorld, feedback, tileSize / 2);
                AddNumberLabel(labelIndex + 1, bWorld, feedback, tileSize / 2);
            }

            feedback.MarkDirtyRepaint();
        }

        private static void AddNumberLabel(int index, Vector2 pos, VisualElement parent, Vector2 offset)
        {
            // Label Setup
            var label = new Label((index + 1).ToString());
            parent.Add(label);

            // Position
            label.style.position = Position.Absolute;
            label.style.top = pos.y + offset.y - 12f;
            label.style.left = pos.x + offset.x - 12f;

            // Text
            label.style.color = Color.black;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;

            // Size
            label.style.width = 24;
            label.style.height = 24;

            // Circle
            label.SetBorderRadius(12f);

            // Border
            Color border = new Color(0, 0, 0, 0.33f);
            label.SetBorder(border, 3f);

            // Background
            label.style.backgroundColor = Color.white;
        }

        private void DrawTriggers(MainView view, TileBundleGroup selected)
        {
            foreach (TileTrigger trigger in selected.Addons.triggers)
            {
                if (!trigger.isVisible) continue;
                GraphElement shape = null;
                switch (trigger.Ttype)
                {
                    case TileTriggerType.Box:
                        shape = DrawBox(view, selected, trigger);
                        break;
                    case TileTriggerType.Circle:
                        shape = DrawCircle(view, selected, trigger);
                        break;
                }

                view.AddElementToLayerContainer(_tgb.OwnerLayer, this, shape);
                shape.SendToBack();
            }

        }

        private GraphElement DrawBox(MainView view, TileBundleGroup selected, TileTrigger trigger)
        {
            var boxfeedback = new AreaFeedback();
            var startPosition = selected.AreaRect.position;

            startPosition.x -= trigger.Range - 1;
            startPosition.y += trigger.Range - 1;

            var endPosition = selected.AreaRect.position;
            endPosition.x += trigger.Range;
            endPosition.y -= trigger.Range;

            Vector2Int aCell = startPosition.ToInt();
            Vector2Int bCell = endPosition.ToInt();

            Vector2 aWorld = _tgb.OwnerLayer.FixedToPosition(aCell, true);
            Vector2 bWorld = _tgb.OwnerLayer.FixedToPosition(bCell, true);

            boxfeedback.SetColor(trigger.areaColor);
            boxfeedback.UpdatePositions(aWorld.ToInt(), bWorld.ToInt());
            return boxfeedback;
        }

        private GraphElement DrawCircle(MainView view, TileBundleGroup selected, TileTrigger trigger)
        {
            Vector2 tileSize = _tgb.OwnerLayer.TileSize * LBSSettings.Instance.general.TileSize;

            Vector2Int cellPos = selected.AreaRect.position.ToInt();

            Vector2 worldTopLeft = _tgb.OwnerLayer.FixedToPosition(cellPos, true);

            Vector2 worldCenter = worldTopLeft + tileSize / 2f;

            float radiusPixels = trigger.Range * tileSize.x;
            var circleFeedback = new CircleFeedback();

            circleFeedback.SetOffset(Vector2.zero);
            circleFeedback.UpdatePositions(worldCenter.ToInt(), (worldCenter + new Vector2(radiusPixels, 0)).ToInt());
            circleFeedback.SetColor(trigger.areaColor);

            return circleFeedback;

        }


        public override void HideVisuals(object target, MainView view) { }
        public override void ShowVisuals(object target, MainView view) { }
        public override void Update(object target, MainView view, Vector2 tesselationSize) { }
    }
}
