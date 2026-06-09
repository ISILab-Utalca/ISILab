using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using ISILab.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class EdgeExteriorTileView : ExteriorTileView
    {
        private static VisualTreeAsset view;

        private LBSCustomPainterCircle leftConnection, rightConnection, topConnection, bottomConnection;
        private LBSPainterVisualElement leftSide, rightSide, topSide, bottomSide;
        private LBSCustomPainterBox center;
        public EdgeExteriorTileView(List<string> connections = null) : base(connections, "ConnectedTile")
        {
            connections ??= new List<string>() { "", "", "", "" };

            if (view == null)
            {
                view = DirectoryTools.GetAssetByName<VisualTreeAsset>("ConnectedTile");
            }
            view.CloneTree(this);

            leftConnection = new LBSCustomPainterCircle();
            rightConnection = new LBSCustomPainterCircle();
            topConnection = new LBSCustomPainterCircle();
            bottomConnection = new LBSCustomPainterCircle();

            center = new LBSCustomPainterBox();

            this.Add(leftConnection);
            this.Add(rightConnection);
            this.Add(topConnection);
            this.Add(bottomConnection);
            this.Add(center);

            leftSide = this.Q<LBSPainterVisualElement>("LeftSide");
            rightSide = this.Q<LBSPainterVisualElement>("RightSide");
            topSide = this.Q<LBSPainterVisualElement>("TopSide");
            bottomSide = this.Q<LBSPainterVisualElement>("BottomSide");


            SetConnections(connections.ToArray());
            EditorApplication.delayCall += () => {

                var centerPoint = new Vector2(50, 50);

                leftConnection.MinPos = centerPoint*Vector2.left + centerPoint;
                rightConnection.MinPos = centerPoint*Vector2.right + centerPoint;
                bottomConnection.MinPos = centerPoint*Vector2.up + centerPoint;
                topConnection.MinPos = centerPoint*Vector2.down + centerPoint;

                center.MinPos = centerPoint / 2f;
                center.MaxPos = centerPoint + (centerPoint / 2f);

                // Force initialization repaint updates
                topConnection.MarkDirtyRepaint();
                rightConnection.MarkDirtyRepaint();
                bottomConnection.MarkDirtyRepaint();
                leftConnection.MarkDirtyRepaint();
                center.MarkDirtyRepaint();

                SetConnections(connections.ToArray());

                this.SetBorder(Color.black, 0);
                style.display = DisplayStyle.Flex;
            };

            style.overflow = Overflow.Hidden;
            style.display = DisplayStyle.None;
        }

        public override void SetConnections(string[] tags)
        {
            List<LBSTag> tts = LBSAssetsStorage.Instance.Get<LBSTag>();
            Color invalidColor = Color.white;
            Color color = invalidColor;
            Dictionary<Color, int> ConnectionColors = new Dictionary<Color, int>();

            if (tags.Length > 0 && !string.IsNullOrEmpty(tags[0]))
            {
                color = tts.Find(t => t.Label.Equals(tags[0])).Color;

                rightConnection.FillColor = color;
                rightSide.BGcolor = BrightenColor(color);
                rightConnection.style.display = DisplayStyle.Flex;

                if (!ConnectionColors.TryAdd(color, 1)) ConnectionColors[color]++;
            }
            else
            {
                rightConnection.FillColor = invalidColor;
                rightSide.BGcolor = invalidColor; // Added to clear background on empty
                rightConnection.style.display = DisplayStyle.None;
            }

            if (tags.Length > 1 && !string.IsNullOrEmpty(tags[1]))
            {
                color = tts.Find(t => t.Label.Equals(tags[1])).Color;

                topConnection.FillColor = color;
                topSide.BGcolor = BrightenColor(color);
                topConnection.style.display = DisplayStyle.Flex;

                if (!ConnectionColors.TryAdd(color, 1)) ConnectionColors[color]++;
            }
            else
            {
                topConnection.FillColor = invalidColor;
                topSide.BGcolor = invalidColor;
                topConnection.style.display = DisplayStyle.None;
            }

            if (tags.Length > 2 && !string.IsNullOrEmpty(tags[2]))
            {
                color = tts.Find(t => t.Label.Equals(tags[2])).Color;

                leftConnection.FillColor = color;
                leftSide.BGcolor = BrightenColor(color);
                leftConnection.style.display = DisplayStyle.Flex;

                if (!ConnectionColors.TryAdd(color, 1)) ConnectionColors[color]++;
            }
            else
            {
                leftConnection.FillColor = invalidColor;
                leftSide.BGcolor = invalidColor;
                leftConnection.style.display = DisplayStyle.None;
            }

            if (tags.Length > 3 && !string.IsNullOrEmpty(tags[3]))
            {
                color = tts.Find(t => t.Label.Equals(tags[3])).Color;

                bottomConnection.FillColor = color;
                bottomSide.BGcolor = BrightenColor(color);
                bottomConnection.style.display = DisplayStyle.Flex;

                if (!ConnectionColors.TryAdd(color, 1)) ConnectionColors[color]++;
            }
            else
            {
                bottomConnection.FillColor = invalidColor;
                bottomSide.BGcolor = invalidColor;
                bottomConnection.style.display = DisplayStyle.None;
            }

            // Paints center if there are connections and to the most connections
            if (ConnectionColors.Count > 0)
            {
                var orderedConnectionColors = ConnectionColors
                    .OrderByDescending(kvp => kvp.Value)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                // Kept SetBackgroundColor here as center uses it uniformly, 
                // but you can change it to center.FillColor = ... if needed.
                center.FillColor = tags.Contains("") ? invalidColor : orderedConnectionColors.First().Key;
            }
            else
            {
                center.FillColor = invalidColor;
            }
        }

        public override void SetTileCenter(LBSTag identifier)
        {
            var color = identifier.Color;
            center.FillColor = color;
            bottomSide.BGcolor = BrightenColor(color);
            topSide.BGcolor = BrightenColor(color);
            leftSide.BGcolor = BrightenColor(color);
            rightSide.BGcolor = BrightenColor(color);
        }

        internal override void SetSelectionMode(bool layerSelected)
        {
            var displayConnection = layerSelected ? DisplayStyle.Flex : DisplayStyle.None; 
            float alpha = layerSelected ? 1.0f : 0f;
            int lineWidth = layerSelected ? 1 : 0;

            leftConnection.style.display = displayConnection;
            topConnection.style.display = displayConnection;
            rightConnection.style.display = displayConnection;
            bottomConnection.style.display = displayConnection;


           // topSide.LineWidth = lineWidth;
           // leftSide.LineWidth = lineWidth;
           // rightSide.LineWidth = lineWidth;
//bottomSide.LineWidth = lineWidth;

         //   center.LineWidth = lineWidth;
        }
    }
}
