using System;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;

namespace ISILab.LBS.Modules
{
    [Serializable]
    public class StairsModule : LBSModule, ISelectable
    {
        private List<LBSStair> _stairs = new();

        public StairsModule(List<LBSStair> stairs = null)
        {
            _stairs = stairs;
        }

        public void AddStair()
        {

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
            throw new NotImplementedException();
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
        private List<Vector2Int> _positions;
        private int _inferiorFloor;
        private int _superiorFloor;
        private int _direction;
        private StairShape _shape;
        #endregion

        #region PROPERTIES
        public List<Vector2Int> Positions => new List<Vector2Int>(_positions);
        public int InferiorFloor => _inferiorFloor;
        public int SuperiorFloor => _superiorFloor;
        public StairShape Shape => _shape;
        #endregion

        public LBSStair(List<Vector2Int> positions, int inferiorFloor, int superiorFloor, StairShape shape)
        {
            _positions = positions;
            _inferiorFloor = inferiorFloor;
            _superiorFloor = superiorFloor;
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