using UnityEngine;

public class DW_TEST : MonoBehaviour

{
    public Renderer targetRenderer;

    [Header("Grid Settings")]
    public int mapWidth = 60;
    public int mapHeight = 60;

    [Header("Walker Settings")]
    public int totalRooms = 6;
    public int walkDistanceBetweenRooms = 5;
    public Vector2Int minRoomSize = new Vector2Int(3, 3);
    public Vector2Int maxRoomSize = new Vector2Int(7, 7);

    [ContextMenu("Generate Drunkard Walk")]
    public void GenerateWalk()
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning("¡Asigna un Renderer!");
            return;
        }

        // 1. Instanciar y generar
        DrunkardWalkerGenerator walker = new DrunkardWalkerGenerator();
        int[,] mapData = walker.Generate(
            mapWidth,
            mapHeight,
            totalRooms,
            walkDistanceBetweenRooms,
            minRoomSize,
            maxRoomSize
        );

        // 2. Usamos el generador de texturas (asegúrate de tener el TextureUtils de la respuesta anterior)
        Texture2D generatedTex = TextureUtils.GenerateMatrixTexture(mapData, 5, true);

        // 3. Aplicar al material
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