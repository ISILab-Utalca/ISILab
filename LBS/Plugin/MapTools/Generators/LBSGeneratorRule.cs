using System;
using System.Collections.Generic;
using ISILab.LBS.Plugin.Core.Settings;
using LBS.Components;
using Newtonsoft.Json;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [System.Serializable]
    public abstract class LBSGeneratorRule : ICloneable
    {
        [JsonIgnore, SerializeField]
        internal Generator3D generator3D;

        public struct GeneratedGO
        {
            public GameObject go;
            public string message;

            public GeneratedGO(GameObject _go, string _message)
            {
                go = _go;
                message = _message;
            }
        }

        public LBSGeneratorRule() { }

        /// <summary>
        /// Generate the GameObject for the layer
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="settings"></param>
        /// <returns>returns a tuple of the generated game object containing all the content, as well as a
        /// string in case the game object is invalid(null)</returns>
        public abstract GeneratedGO Generate(LBSLayer layer, LBSGenerator3DSettings settings); //Falta modificar las reescrituras

        /// <summary>
        /// Check if the layer is viable to be generated
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public abstract List<Message> CheckViability(LBSLayer layer);

        /// <summary>
        /// Clone this object to obtain a new instance of this object
        /// </summary>
        /// <returns></returns>
        public abstract object Clone();
    }

    public class Message
    {
        public enum Type
        {
            Error,
            Warning,
            Info
        }

        public Type type;
        public string msg;

        public Message(Type type, string msg)
        {
            this.type = type;
            this.msg = msg;
        }
    }
}
