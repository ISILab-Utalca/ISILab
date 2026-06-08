using ISILab.Commons.Utility.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Internal;
using ISILab.LBS.CustomComponents;
using ISILab.Extensions;
using UnityEditor;

namespace ISILab.LBS.VisualElements
{
    public class VertexExteriorTileView : ExteriorTileView
    {
        private static VisualTreeAsset view;

        private CustomComponents.LBSCustomPainter upperRightFill;
        private CustomComponents.LBSCustomPainter upperLeftFill;
        private CustomComponents.LBSCustomPainter lowerLeftFill;
        private CustomComponents.LBSCustomPainter lowerRightFill;

        //private VisualElement fill, center;
        private CustomComponents.LBSCustomPainter center;

        readonly Color invalidColor = Color.white;
        float boderWidth = 1f;
        public VertexExteriorTileView(List<string> connections = null) : base(connections, "ConnectedVertexBasedTile")
        {
            connections ??= new List<string>() { "", "", "", "" };

            if (view == null)
            {
                view = DirectoryTools.GetAssetByName<VisualTreeAsset>("ConnectedVertexBasedTile");
            }
            view.CloneTree(this);

            upperLeftFill = new LBSCustomPainter();
            upperRightFill = new LBSCustomPainter();
            lowerLeftFill = new LBSCustomPainter();
            lowerRightFill = new LBSCustomPainter();
            center = new LBSCustomPainter();
            
            this.Add(upperRightFill);
            this.Add(upperLeftFill);
            this.Add(lowerLeftFill);
            this.Add(lowerRightFill);
            this.Add(center);

            EditorApplication.delayCall += () => {
               
                var centerPoint = new Vector2(50, 50);
                var maxBounds = new Vector2(100, 100);

                upperLeftFill.MinPos = Vector2.zero;
                upperLeftFill.MaxPos = centerPoint;

                upperRightFill.MinPos = new Vector2(centerPoint.x, 0);
                upperRightFill.MaxPos = new Vector2(maxBounds.x, centerPoint.y);

                lowerLeftFill.MinPos = new Vector2(0, centerPoint.y);
                lowerLeftFill.MaxPos = new Vector2(centerPoint.x, maxBounds.y);

                lowerRightFill.MinPos = centerPoint;
                lowerRightFill.MaxPos = maxBounds;

                center.MinPos = centerPoint / 2f;
                center.MaxPos = centerPoint + (centerPoint / 2f);

                // Force initialization repaint updates
                upperLeftFill.MarkDirtyRepaint();
                upperRightFill.MarkDirtyRepaint();
                lowerLeftFill.MarkDirtyRepaint();
                lowerRightFill.MarkDirtyRepaint();
                center.MarkDirtyRepaint();

                SetConnections(connections.ToArray());
            };

            
        }


       
        public override void SetConnections(string[] tags)
        {
            var tts = LBSAssetsStorage.Instance.Get<LBSTag>();

            Color color = invalidColor;
            Dictionary<Color, int> ConnectionColors = new Dictionary<Color, int>();

            if (!string.IsNullOrEmpty(tags[0]))
            {
                color = tts.Find(t => t.Label.Equals(tags[0])).Color;
                (upperRightFill as LBSCustomPainter).FillColor = BrightenColor(color);

                if (!ConnectionColors.TryAdd(color, 1)) ConnectionColors[color]++;
            }
            else
            {
                SetBackgroundColor(upperRightFill, invalidColor);
            }

            if (!string.IsNullOrEmpty(tags[1]))
            {
                color = tts.Find(t => t.Label.Equals(tags[1])).Color;
                (upperLeftFill as LBSCustomPainter).FillColor = BrightenColor(color);
                
                if (!ConnectionColors.TryAdd(color, 1)) ConnectionColors[color]++;
            }
            else
            {
                SetBackgroundColor(upperLeftFill, invalidColor);
            }

            if (!string.IsNullOrEmpty(tags[2]))
            {
                color = tts.Find(t => t.Label.Equals(tags[2])).Color;
                (lowerLeftFill as LBSCustomPainter).FillColor = BrightenColor(color);

                if (!ConnectionColors.TryAdd(color, 1)) ConnectionColors[color]++;
            }
            else
            {
                SetBackgroundColor(lowerLeftFill, invalidColor);
            }

            if (!string.IsNullOrEmpty(tags[3]))
            {
                color = tts.Find(t => t.Label.Equals(tags[3])).Color;
                (lowerRightFill as LBSCustomPainter).FillColor = BrightenColor(color);

                if (!ConnectionColors.TryAdd(color, 1)) ConnectionColors[color]++;
            }
            else
            {
                SetBackgroundColor(lowerRightFill, invalidColor);
            }

            // paints center if there are connections and to the most connections
            if (ConnectionColors.Count > 0)
            {
                var orderedConnectionColors = ConnectionColors
                    .OrderByDescending(kvp => kvp.Value)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                (center as LBSCustomPainter).FillColor = orderedConnectionColors.First().Key;
            }
            else
            {
                SetBackgroundColor(center, invalidColor);
            }
        }

        //public override void SetTileCenter(LBSTag identifier)
        //{
        //    var color = identifier.Color;
        //    SetBackgroundColor(center, color);
        //    SetImageTint(bottomSide, BrightenColor(color));
        //    SetImageTint(topSide, BrightenColor(color));
        //    SetImageTint(leftSide, BrightenColor(color));
        //    SetImageTint(rightSide, BrightenColor(color));
        //}
    }
}