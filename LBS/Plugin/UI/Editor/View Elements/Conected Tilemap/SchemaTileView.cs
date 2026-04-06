using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using LBS.Components;
using LBS.Components.TileMap;
using LBS.VisualElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class SchemaTileView : GraphElement
    {
        #region VIEW FIELDS
        private static VisualTreeAsset view;

        private const float borderThickness = 2f;
        private const float backgroundOpacity = 0.2f;
        
        private VisualElement left;
        private VisualElement right;
        private VisualElement top;
        private VisualElement bottom;
        private VisualElement border;
        #endregion

        SchemaTileConnectionView leftConnectionView;
        SchemaTileConnectionView rightConnectionView;
        SchemaTileConnectionView topConnectionView;
        SchemaTileConnectionView bottomConnectionView;

        private LBSLayer _ownerLayer;

        public SchemaTileView(LBSLayer ownerLayer)
        {
            if (view == null)
            {
                view = DirectoryTools.GetAssetByName<VisualTreeAsset>("SchemaTileView");
            }
            view.CloneTree(this);

            left = this.Q<VisualElement>("Left");
            right = this.Q<VisualElement>("Right");
            top = this.Q<VisualElement>("Top");
            bottom = this.Q<VisualElement>("Bottom");
            border = this.Q<VisualElement>("Border");

            this.SetMargins(0);
            this.SetPaddings(0);
            this.SetBorderRadius(0);
            this.SetBorder(Color.black, 1);

            _ownerLayer = ownerLayer;
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

        public void SetBackgroundColor(Color color)
        {
            color.a = backgroundOpacity;
            border.style.backgroundColor = color;
            right.style.backgroundColor = color;
            top.style.backgroundColor = color;
            left.style.backgroundColor = color;
            bottom.style.backgroundColor = color;
        }

        public void SetBorderColor(Color color, float thickness)
        {
            border.style.borderRightColor = color;
            border.style.borderLeftColor = color;
            border.style.borderTopColor = color;
            border.style.borderBottomColor = color;
            this.SetBorder(color, thickness);
        }

        public void SetConnections(string[] tags)
        {
            right.SetDisplay(false);
            top.SetDisplay(false);
            left.SetDisplay(false);
            bottom.SetDisplay(false);

            border.style.borderRightWidth =
                tags[LBSDirection.ToInt(LBSDirection.Right)].Equals(SchemaBehaviour.Empty) ? 0f : borderThickness;
            border.style.borderTopWidth =
                tags[LBSDirection.ToInt(LBSDirection.Up)].Equals(SchemaBehaviour.Empty) ? 0f : borderThickness;
            border.style.borderLeftWidth =
                tags[LBSDirection.ToInt(LBSDirection.Left)].Equals(SchemaBehaviour.Empty) ? 0f : borderThickness;
            border.style.borderBottomWidth =
                tags[LBSDirection.ToInt(LBSDirection.Down)].Equals(SchemaBehaviour.Empty) ? 0f : borderThickness;
        }

        public void CreateConnectionView(LBSLayer layer, LBSTile tile, string connectionType, Vector2 pos, string key)
        {
            var connectionView = new SchemaTileConnectionView(layer, tile, connectionType, key)
            {
                style =
                {
                    width = 64,
                    height = 64,
                    backgroundColor = Color.clear,
                    position = Position.Absolute,
                    left = pos.x-2.5f,
                    top = pos.y-2.5f
                }
            };

            switch (key)
            {
                case LBSDirection.Right:
                    rightConnectionView = connectionView;
                    break;

                case LBSDirection.Up:
                    topConnectionView = connectionView;
                    break;

                case LBSDirection.Left:
                    leftConnectionView = connectionView;
                    break;

                case LBSDirection.Down:
                    bottomConnectionView = connectionView;
                    break;

                default:
                    Debug.LogWarning("Wrong key type");
                    break;
            }

            Add(connectionView);
        }

        public void RemoveConnectionViews()
        {
            if (rightConnectionView     is not null) Remove(rightConnectionView);
            if (topConnectionView       is not null) Remove(topConnectionView);
            if (leftConnectionView      is not null) Remove(leftConnectionView);
            if (bottomConnectionView    is not null) Remove(bottomConnectionView);

            rightConnectionView = topConnectionView = leftConnectionView = bottomConnectionView = null;
        }

        /// <summary>
        /// returns a dictionary of the connection points, where:
        /// - key = direction (example: "up")
        /// - value = connection type (example: "door")
        ///
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static Dictionary<string,string> GetConnectionPoints(List<string> tags)
        {
            Dictionary<string, string> ConnectionPoints = new Dictionary<string, string>();

            int indexRight = LBSDirection.ToInt(LBSDirection.Right);
            int indexUp = LBSDirection.ToInt(LBSDirection.Up);
            int indexLeft = LBSDirection.ToInt(LBSDirection.Left);
            int indexDown = LBSDirection.ToInt(LBSDirection.Down);
            if (tags.Count > indexRight)
            {
                ConnectionPoints.Add(LBSDirection.Right, tags[indexRight]); 
            }
            if(tags.Count > indexUp)
            {
                ConnectionPoints.Add(LBSDirection.Up, tags[indexUp]);
            }
            if (tags.Count > indexLeft) 
            {
                ConnectionPoints.Add(LBSDirection.Left, tags[indexLeft]);
            } 
            if(tags.Count > indexDown)
            {
                ConnectionPoints.Add(LBSDirection.Down, tags[indexDown]);
            }
            return ConnectionPoints; 
        }


    }
}