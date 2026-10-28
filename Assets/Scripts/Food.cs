using System.Collections.Generic;
using UnityEngine;

public class Food : MonoBehaviour
{
    [Header("Позиція на сітці")]
    public Vector2Int gridPosition;

    [Header("Межі спавну (у клітинках)")]
    public Vector2Int minBounds = new Vector2Int(-10, -5);
    public Vector2Int maxBounds = new Vector2Int(10, 5);

    private float _gridSize = 1f;

    public void Init(float gridSize)
    {
        _gridSize = gridSize;
    }

    // Переміщує яблуко на вільну клітинку
    public void Respawn(List<Vector2Int> occupiedPositions)
    {
        List<Vector2Int> availablePositions = new List<Vector2Int>();

        // Перебираємо всі клітинки арени
        for (int x = minBounds.x; x <= maxBounds.x; x++)
        {
            for (int y = minBounds.y; y <= maxBounds.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                // Якщо в цій клітинці немає тіла чи голови змійки — вона доступна
                if (!occupiedPositions.Contains(pos))
                {
                    availablePositions.Add(pos);
                }
            }
        }

        // Якщо вільні клітинки є — обираємо випадкову
        if (availablePositions.Count > 0)
        {
            int randomIndex = Random.Range(0, availablePositions.Count);
            gridPosition = availablePositions[randomIndex];

            transform.position = new Vector3(gridPosition.x * _gridSize, gridPosition.y * _gridSize, 0f);
        }
    }
}