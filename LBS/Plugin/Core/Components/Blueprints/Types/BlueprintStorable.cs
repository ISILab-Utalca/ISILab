using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class BlueprintStorable
    {
        #region FIELDS
        /* To find and add data or create layer from scratch */
        [SerializeField]
        string layerName;
        /* Store the layer template type (Interior, Quest, Population, etc.) 
         * and use it to create the given type if the layer does not exist and then
         * load the stored data from the blueprint
        */
        [SerializeField]
        string layerID;

        /*List of object(Module, behavior, assistant, etc) and their content of objects*/
        [SerializeField]
        List<BlueprintData> data;
        #endregion

        #region PROPERTIES
        public string LayerName { get => layerName; set => layerName = value; }
        public string LayerID { get => layerID; set => layerID = value; }
        public List<BlueprintData> Data { get => data; set => data = value; }
        #endregion

        public BlueprintStorable(string layerName, string layerTemplateGUID, BlueprintData[] objs)
        {
            if (objs == null || !objs.Any()) return;
            
            LayerName = layerName;
            LayerID = layerTemplateGUID;
            Data = new List<BlueprintData>(objs);
        }
    }
}
