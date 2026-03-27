using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;

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

        #region INHERITED METHODS
        // ISelectable Methods
        public List<object> GetSelected(Vector2Int position)
        {
            return new List<object>();
            //throw new NotImplementedException();
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
        private StairShape _shape;
        #endregion

        #region PROPERTIES
        public List<Vector2Int> Positions => new List<Vector2Int>(_positions);
        public int InferiorFloor => _inferiorFloor;
        public int SuperiorFloor => _superiorFloor;
        public int Direction => _direction;
        public StairShape Shape => _shape;
        #endregion

        public LBSStair(List<Vector2Int> positions, int inferiorFloor, int superiorFloor, 
            int direction, StairShape shape)
        {
            _positions = positions;
            _inferiorFloor = inferiorFloor;
            _superiorFloor = superiorFloor;
            _direction = direction;
            _shape = shape;
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