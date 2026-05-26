using UnityEngine;
using ISILab.Commons.Utility.Editor;

using ISILab.LBS.Plugin.MapTools.Editor.Templates;
using System.Collections.Generic;
using LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.VisualElements;








#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ISILab.LBS.Editor.Utilities
{
    public static class TextureMapper
    {
#if UNITY_EDITOR
        [MenuItem("Assets/Read Texture as Map")]
        private static void MapTexture()
        {
            Texture2D tex = Selection.activeObject as Texture2D;

            Color white = new Color(229f/255f, 229f / 255f, 229f / 255f);
            int texSize = tex.width;
            int mapSize = 128;
            int blockSize = texSize / mapSize;
            char[,] map = new char[mapSize, mapSize];
            if (tex == null) return;
            for(int y = 0; y < mapSize; y++)
            {
                for(int x = 0; x < mapSize; x++)
                {
                    int t = 0, f = 0;
                    Color[] pixels = tex.GetPixels(y * blockSize, x * blockSize, blockSize, blockSize);
                    foreach(Color pixel in pixels)
                    {
                        if (pixel == white)
                            t++;
                        else f++;
                    }
                    map[x, y] = t < f ? '0' : '1';
                }
            }
            string l = "";
            for(int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    l += map[x, y];
                }
                l += "\n";
            }
            Debug.Log(l);

            GenerateMap(map, mapSize);
        }

        private static void GenerateMap(char[,] map, int size)
        {
            var lvlData = LBS.loadedLevel.data;
            if(lvlData.LayerCount > 0)
            {
                Debug.LogWarning("Use only empty levels when generating a map from a texture.");
                return;
            }
            List<LayerTemplate> templates = DirectoryTools.GetScriptablesByType<LayerTemplate>();

            LayerTemplate interior = templates.Find(t => t.layer.ID.Equals("Interior"));

            if (interior.layer.Clone() is not LBSLayer layer) return;
            LBSMainWindow.Instance.layerPanel.AddLayer(layer);
            SchemaBehaviour schema = layer.GetBehaviour<SchemaBehaviour>();
            Zone zone = schema.AddZone();
            for(int i = 0; i < size; i++)
            {
                for(int j = 0; j < size; j++)
                {
                    if (map[i,j] == '1')
                    {
                        var tile = schema.AddTile(new Vector2Int(j, i), zone); 
                        schema.AddConnections(
                        tile,
                        new List<string>() { "", "", "", "" },
                        new List<bool> { true, true, true, true }
                        );
                    }
                }
            }

            schema.RecalculateWalls();

            if (layer is null) return;

            if (LBS.loadedLevel != null) EditorUtility.SetDirty(LBS.loadedLevel);
            layer.OnChangeUpdate();
            if (DrawManager.Instance != null) DrawManager.Instance.RedrawLayer(layer);

            LBSMainWindow.Instance._selectedLayer = layer;
            if (LBSInspectorPanel.Instance != null)
            {
                LBSInspectorPanel.Instance.SetTarget(layer);
                LBSInspectorPanel.ActivateBehaviourTab();
            }
        }
#endif
    }
}
