using UnityEngine;

public class BSP_TEST : MonoBehaviour
{
    public Renderer targetRenderer;

    [Header("BSP Settings")]
    public int mapWidth = 50;
    public int mapHeight = 50;
    public int minPartitionSize = 10;
    public int minRoomSize = 4;

    [ContextMenu("Generate BSP Dungeon")]
    public void GenerateDungeon()
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning("Por favor, asigna un Renderer en el inspector.");
            return;
        }

        BSPDungeonGenerator bsp = new BSPDungeonGenerator();
        int[,] mapData = bsp.Generate(mapWidth, mapHeight, minPartitionSize, minRoomSize);

        Texture2D generatedTex = TextureUtils.GenerateMatrixTexture(mapData, 5, true);

        if (Application.isPlaying)
        {
            targetRenderer.material.mainTexture = generatedTex;
        }
        else
        {
            targetRenderer.sharedMaterial.mainTexture = generatedTex;
        }
    }
}