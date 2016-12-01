using UnityEngine;
using System.Collections;

namespace CreativeSpore.SuperTilemapEditor
{

    public class BrushUtil
    {
        public static Vector2 GetSnappedPosition(Vector2 position, Vector2 cellSize)
        {
            Vector2 centerCell = position - cellSize / 2f;
            Vector2 snappedPos = new Vector2
            (
                Mathf.Round(centerCell.x / cellSize.x) * cellSize.x,
                Mathf.Round(centerCell.y / cellSize.y) * cellSize.y
            );
            return snappedPos;
        }

        public static int GetGridX(Vector2 position, Vector2 cellSize)
        {
            float x = position.x > 0f ? position.x + Vector2.kEpsilon : position.x - float.Epsilon;
            return (int)Mathf.Round((x - cellSize.x / 2f) / cellSize.x);
        }

        public static int GetGridY(Vector2 position, Vector2 cellSize)
        {
            float y = position.y > 0f ? position.y + Vector2.kEpsilon : position.y - float.Epsilon;
            return (int)Mathf.Round((y + float.Epsilon - cellSize.y / 2f) / cellSize.y);
        }
    }
}