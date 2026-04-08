using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

namespace ISILab.LBS.Modules
{
    [Serializable]
    public class StairsModule : LBSModule, ISelectable
    {
        [SerializeField, JsonRequired, SerializeReference]
        private List<LBSStair> _stairs = new();
        public List<LBSStair> Stairs => new List<LBSStair>(_stairs);

        public StairsModule()
        {
        }
        public StairsModule(List<LBSStair> stairs = null)
        {
            _stairs = stairs;
        }

        public void AddStair(LBSStair stair)
        {
            _stairs.Add(stair);
        }

        public bool IsPositionOccupied(Vector2Int position)
        {
            foreach(var stair in _stairs)
            {
                foreach(var pos in stair.Positions)
                {
                    if(position == pos) return true;
                }
            }
            return false;
        }

        public LBSStair GetStairByPoint(Vector2Int position)
        {
            foreach (var stair in _stairs)
            {
                foreach (var pos in stair.Positions)
                {
                    if (position == pos) return stair;
                }

                Vector2Int entranceDir = stair.Positions[0] - stair.Positions[1];
                for (int i = 1; i <= stair.Height; i++)
                {
                    if (position == stair.Positions[0] + entranceDir * i)
                    {
                        return stair;
                    }
                }
            }
            return null;
        }

        public LBSStair GetStairByStartingPoint(Vector2Int startingPoint)
        {
            foreach (var stair in _stairs)
            {
                if (stair.Direction > 0)
                {
                    if (startingPoint == stair.Positions[0])
                        return stair;
                }
                else
                {
                    if (startingPoint == stair.Positions[stair.Positions.Count - 1]) 
                        return stair;
                }
            }
            return null;
        }

        public bool RemoveStair(LBSStair stair)
        {
            bool a = _stairs.Remove(stair);
            return a;
        }


        #region INHERITED METHODS
        // ISelectable Methods
        public List<object> GetSelected(Vector2Int position)
        {
            var pos = OwnerLayer.ToFixedPosition(position);
            var r = new List<object>();
            var stair = GetStairByPoint(pos);

            if (stair != null)
            {
                r.Add(stair);
            }
            return r;
        }

        // LBSModule Methods
        public override void Clear()
        {
            _stairs = new List<LBSStair>();
        }
        public override bool IsEmpty()
        {
            return _stairs.Count == 0;
        }
        public override object Clone()
        {
            StairsModule sm = new StairsModule(new List<LBSStair>(_stairs));
            return sm;
        }
        #endregion
    }

    [Serializable]
    public class LBSStair
    {
        #region FIELDS
        [SerializeField, JsonRequired, SerializeReference]
        private List<Vector2Int> _positions = new();

        [SerializeField, JsonRequired, SerializeReference]
        private int _inferiorFloor;
        [SerializeField, JsonRequired, SerializeReference]
        private int _superiorFloor;
        [SerializeField, JsonRequired, SerializeReference]
        private int _direction;
        [SerializeField, JsonRequired, SerializeReference]
        private int _height = 1;

        [SerializeField, JsonRequired, SerializeReference]
        private Color _color;
        [SerializeField, JsonRequired, SerializeReference]
        private StairShape _shape; // unused

        [SerializeField, JsonRequired]
        private List<string> _styles = new List<string>();
        #endregion

        #region PROPERTIES
        public List<Vector2Int> Positions => new List<Vector2Int>(_positions);

        public int InferiorFloor => _inferiorFloor;
        public int SuperiorFloor => _superiorFloor;
        public int Direction => _direction;
        public int Height 
        { 
            get => _height; 
            set 
            {
                _height = value;
            }
        }

public Color Color 
        { 
            get => _color; 
            set 
            {
                _color = value;
                OnVisualChange?.Invoke(this);
            }
        }
        public StairShape Shape => _shape; // unused

        public List<string> Styles
        {
            get => _styles;
            set => _styles = value;
        }
        #endregion

        #region CALLBACKS
        public Action<LBSStair> OnVisualChange;
        #endregion

        public LBSStair(List<Vector2Int> positions, int inferiorFloor, int superiorFloor, 
            int direction, StairShape shape)
        {
            _positions = positions;
            _inferiorFloor = inferiorFloor;
            _superiorFloor = superiorFloor;
            _direction = direction;
            _shape = shape;

            var darkGray = new Color(0.25f, 0.25f, 0.25f, 1);
            _color = darkGray;
        }

        public LBSStair Inverted()
        {
            return new LBSStair(_positions, _inferiorFloor, _superiorFloor, -_direction, _shape);
        }

        public override bool Equals(object obj)
        {
            if (obj is not LBSStair) return false;

            LBSStair other = obj as LBSStair;
            bool samePositions = _positions.Count == other.Positions.Count;
            if (samePositions)
            {
                for(int i = 0; i < _positions.Count; i++)
                {
                    if (_positions[i] != other.Positions[i])
                    {
                        samePositions = false;
                        break;
                    }
                }
            }

            return samePositions &&
                _inferiorFloor.Equals(other.InferiorFloor) &&
                _superiorFloor.Equals(other.SuperiorFloor) &&
                _direction.Equals(other.Direction) &&
                _shape.Equals(other.Shape);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public static class ZoneExtension
    {
        public static List<Bundle> GetStyleBundles(this LBSStair stair)
        {
            var bundles = new List<Bundle>();
            var allBundles = LBSAssetsStorage.Instance.Get<Bundle>().ToList();
            foreach (var tags in stair.Styles)
            {
                bundles.Add(allBundles.Find(b => b.name.Equals(tags)));
            }
            return bundles;
        }
    }

    [Serializable]
    public enum StairShape
    {
        None,
        Straight,
        Corner,
        U_Shape,
        S_Shape
    }
}