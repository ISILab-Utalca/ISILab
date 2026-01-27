using System;
using LBS.Components;
using Newtonsoft.Json;
using UnityEngine;


namespace ISILab.LBS.Modules
{
    [Serializable]
    public class LBSNote : ICloneable
    {
        #region FIELDS

        [SerializeField, JsonRequired] private string id = "";

        private static int noteCounter = 0;

        [SerializeField, JsonRequired] protected float x;

        [SerializeField, JsonRequired] protected float y;

        [SerializeField, JsonRequired] protected string message;

        [SerializeField, JsonRequired, HideInInspector]
        private LBSLayer ownerLayer;

        #endregion

        #region PROPERTIES

        public string ID
        {
            get => id;
            set => id = value;
        }

        public Vector2 Position
        {
            get => new(x, y);
            set
            {
                x = value.x;
                y = value.y;
            }
        }

        public string Message
        {
            get => message;
            set => message = value;
        }

        public LBSLayer OwnerLayer
        {
            get => ownerLayer;
            set => ownerLayer = value;
        }

        #endregion

        #region CONSTRUCTORS

        protected LBSNote()
        {
        }

        public LBSNote(Vector2 position, string message, LBSLayer ownerLayer)
        {
            id = $"Note {++noteCounter}";
            x = position.x;
            y = position.y;
            this.message = message;
            this.ownerLayer = ownerLayer;
        }

        #endregion

        public object Clone()
        {
            var clone = new LBSNote();

            clone.ID = ID;
            clone.x = x;
            clone.y = y;
            clone.message = message;

            return clone;
        }
    }
}
