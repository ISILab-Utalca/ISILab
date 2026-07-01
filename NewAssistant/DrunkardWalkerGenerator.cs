using ISILab.Commons.Utility;
using UnityEngine;

public class DrunkardWalkerGenerator
{
    private int[,] map;
    private int mapWidth;
    private int mapHeight;

    // Direcciones: 0=Arriba, 1=Derecha, 2=Abajo, 3=Izquierda
    private readonly int[] dx = { 0, 1, 0, -1 };
    private readonly int[] dy = { 1, 0, -1, 0 };

    /// <summary>
    /// Genera la mazmorra. 0 = Vacío, 1 = Pasillos, 2+ = Habitaciones.
    /// </summary>
    public int[,] Generate(int width, int height, int roomCount, int walkDistance, Vector2Int minRoom, Vector2Int maxRoom)
    {
        mapWidth = width;
        mapHeight = height;
        // En C#, los arreglos de enteros se inicializan automáticamente con 0 (espacio vacío)
        map = new int[mapWidth, mapHeight];

        // Empezamos en el centro de la matriz
        int x = mapWidth / 2;
        int y = mapHeight / 2;

        int currentRoomId = 2; // Las habitaciones comienzan con el número 2
        int lastDir = -1;

        for (int i = 0; i < roomCount; i++)
        {
            // 1. Crear la habitación en la posición actual
            CarveRoom(x, y, minRoom, maxRoom, currentRoomId);
            currentRoomId++; // Siguiente habitación tendrá un número distinto

            // Si ya pusimos la última habitación, detenemos el caminante
            if (i == roomCount - 1) break;

            // 2. Elegir UNA dirección para trazar todo el pasillo
            int dir = GetRandomDirection(lastDir);
            lastDir = dir;

            // 3. Caminar en línea recta la distancia exacta del pasillo
            for (int step = 0; step < walkDistance; step++)
            {
                int nx = x + dx[dir];
                int ny = y + dy[dir];

                // Si el caminante choca con el límite del mapa, detenemos este pasillo
                if (nx <= 0 || nx >= mapWidth - 1 || ny <= 0 || ny >= mapHeight - 1)
                {
                    break;
                }

                x = nx;
                y = ny;

                // 4. Marcar el pasillo (1) SOLO si la casilla actual está vacía (0)
                // Esto garantiza que el pasillo no borre los números de las habitaciones previas
                if (map[x, y] == 0)
                {
                    map[x, y] = 1;
                }
            }
        }

        return map;
    }

    /// <summary>
    /// Devuelve una dirección aleatoria (0-3), asegurando que no sea exactamente la opuesta a la anterior.
    /// </summary>
    private int GetRandomDirection(int lastDir)
    {
        int dir;
        do
        {
            dir = SafeRandom.Range(0, 4);
        }
        // Condición: Evitar que se devuelva por donde vino (giro de 180 grados)
        while (lastDir != -1 && dir == (lastDir + 2) % 4);

        return dir;
    }

    /// <summary>
    /// Dibuja la habitación en la matriz usando el roomId proporcionado.
    /// </summary>
    private void CarveRoom(int cx, int cy, Vector2Int minRoom, Vector2Int maxRoom, int roomId)
    {
        int w = SafeRandom.Range(minRoom.x, maxRoom.x + 1);
        int h = SafeRandom.Range(minRoom.y, maxRoom.y + 1);

        int startX = cx - w / 2;
        int startY = cy - h / 2;

        for (int px = startX; px < startX + w; px++)
        {
            for (int py = startY; py < startY + h; py++)
            {
                // Asegurarnos de que no intentamos pintar fuera de los límites de la matriz
                if (px > 0 && px < mapWidth - 1 && py > 0 && py < mapHeight - 1)
                {
                    map[px, py] = roomId;
                }
            }
        }
    }
}