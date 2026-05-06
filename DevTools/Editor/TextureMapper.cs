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
            for(int i = 0; i < mapSize; i++)
            {
                for(int j = 0; j < mapSize; j++)
                {
                    int t = 0, f = 0;
                    Color[] pixels = tex.GetPixels(i * blockSize, j * blockSize, blockSize, blockSize);
                    foreach(Color pixel in pixels)
                    {
                        if (pixel == white)
                            t++;
                        else f++;
                    }
                    map[i, j] = t < f ? '0' : '1';
                }
            }
            string l = "";
            for(int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    l += map[i, j];
                }
                l += "\n";
            }
            Debug.Log(l);
        }
#endif
    }
}
