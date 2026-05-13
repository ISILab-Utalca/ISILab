using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ISILab.DevTools.Editor
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
        }
#endif
    }
}
