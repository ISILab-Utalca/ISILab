using LBS.Components;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Components
{
    public class Blueprint : ScriptableObject
    {
        [SerializeField] 
        private string blueprintName = "New Blueprint";

        [SerializeField, HideInInspector]
        private byte[] previewImageData;

        [SerializeField, HideInInspector]
        private int previewWidth;

        [SerializeField, HideInInspector]
        private int previewHeight;

        private Texture2D previewImageCache;

        [SerializeField]
        private Vector2Int size;

        // Store all of the layer data types
        [SerializeField, SerializeReference]
        private List<LBSLayer> layers;

        public string BlueprintName
        {
            get => blueprintName;
            set
            {
                blueprintName = value;
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }

        public List<LBSLayer> Layers
        {
            get => layers;

            set => layers = value;
        }

        public Texture2D PreviewImage
        {
            get
            {
                if (previewImageCache == null && previewImageData != null && previewImageData.Length > 0)
                {
                    previewImageCache = new Texture2D(previewWidth, previewHeight);
                    previewImageCache.LoadImage(previewImageData);
                }

                return previewImageCache;
            }
            set
            {
                if (value == null)
                {
                    previewImageData = null;
                    previewImageCache = null;
                    return;
                }

                previewWidth = value.width;
                previewHeight = value.height;
                previewImageData = value.EncodeToPNG();

                previewImageCache = value;

                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }

        public Vector2Int Size
        {
            get { return size; }
            set { size = value; }
        }
    }
}
