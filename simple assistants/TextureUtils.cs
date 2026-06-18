using UnityEngine;

public static class TextureUtils
{
    /// <summary>
    /// Generates a texture from a 2D integer matrix, assigning a random color to each number.
    /// </summary>
    /// <param name="matrix">The matrix of numbers.</param>
    /// <param name="cellSize">The size in pixels for each matrix cell (e.g., 3).</param>
    /// <param name="drawGrid">If true, draws black lines to separate the cells.</param>
    /// <returns>The generated Texture2D.</returns>
    public static Texture2D GenerateMatrixTexture(int[,] matrix, int cellSize, bool drawGrid = true)
    {
        int cols = matrix.GetLength(0);
        int rows = matrix.GetLength(1);

        int texWidth = cols * cellSize;
        int texHeight = rows * cellSize;

        // Disable mipmaps (false) because they are not needed for UI/grid textures
        Texture2D texture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);

        // FilterMode.Point keeps the edges sharp (pixel perfect)
        texture.filterMode = FilterMode.Point;

        Color[] pixels = new Color[texWidth * texHeight];

        // Save the original random state to prevent side effects on other game mechanics
        Random.State originalState = Random.state;

        // Loop through each cell in the matrix
        for (int matrixY = 0; matrixY < rows; matrixY++)
        {
            for (int matrixX = 0; matrixX < cols; matrixX++)
            {
                int cellValue = matrix[matrixX, matrixY];

                // Seed the random generator with the cell value. 
                // This guarantees the same number always gets the same random color, without needing a map.
                Random.InitState(cellValue);
                Color cellColor = new Color(Random.value, Random.value, Random.value, 1f);

                // Fill the actual pixels for this specific cell
                for (int py = 0; py < cellSize; py++)
                {
                    for (int px = 0; px < cellSize; px++)
                    {
                        int x = (matrixX * cellSize) + px;
                        int y = (matrixY * cellSize) + py;

                        // Determine if this pixel is a border (left, bottom, or extreme right/top)
                        bool isGridLine = drawGrid && (
                            px == 0 ||
                            py == 0 ||
                            x == texWidth - 1 ||
                            y == texHeight - 1
                        );

                        int pixelIndex = y * texWidth + x;

                        if (isGridLine)
                        {
                            pixels[pixelIndex] = Color.black;
                        }
                        else
                        {
                            pixels[pixelIndex] = cellColor;
                        }
                    }
                }
            }
        }

        // Restore the original random state
        Random.state = originalState;

        // Apply all pixels at once (much faster performance)
        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }
}