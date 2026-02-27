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

        // Store all of the layer data types
        [SerializeField, SerializeReference]
        private List<BlueprintStorable> storableData = new();

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

        public List<BlueprintStorable> StorableData
        {
            get => storableData;
            set
            {
                storableData = value;
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
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

        public Vector2Int GetSize()
        {
            if (storableData == null || storableData.Count == 0)
                return Vector2Int.zero;

            Vector2Int globalMin = new Vector2Int(int.MaxValue, int.MaxValue);
            Vector2Int globalMax = new Vector2Int(int.MinValue, int.MinValue);

            foreach (var sd in storableData)
            {
                foreach (var obj in sd.Data)
                {
                    globalMin = Vector2Int.Min(globalMin, obj.Min);
                    globalMax = Vector2Int.Max(globalMax, obj.Max);
                }
            }

            return globalMax - globalMin;
        }
    }
}
