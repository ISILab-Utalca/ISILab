using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Components
{
    public class Blueprint : ScriptableObject
    {
        [SerializeField] 
        private string blueprintName = "New Blueprint";

        [SerializeField, HideInInspector]
        private Texture2D previewImage;

        // Store all of the layer data types
        [SerializeField, HideInInspector]
        private List<BlueprintStorable> storableData = new();

        public string BlueprintName
        {
            get => blueprintName;
            set => blueprintName = value;
        }

        public List<BlueprintStorable> StorableData
        {
            get => storableData;
            set => storableData = value;
        }

        public Texture2D PreviewImage
        {
            get => previewImage;
            set => previewImage = value;
        }
    }
}
