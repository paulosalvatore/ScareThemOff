using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace CreativeSpore.SuperTilemapEditor
{
    public class RandomBrush : TilesetBrush
    {
        public List<uint> RandomTiles = new List<uint>();

        #region IBrush

        public override uint PreviewTileData()
        {
            return RandomTiles.Count > 0 ? RandomTiles[0] : Tileset.k_TileData_Empty;
        }

        public override uint Refresh(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            if (RandomTiles.Count > 0)
            {
                int randIdx = UnityEngine.Random.Range(0, RandomTiles.Count);
                uint brushTileData = RefreshLinkedBrush(tilemap, gridX, gridY, RandomTiles[randIdx]);
                // overwrite flags
                brushTileData &= ~Tileset.k_TileDataMask_Flags;
                brushTileData |= RandomTiles[randIdx] & Tileset.k_TileDataMask_Flags;
                // overwrite brush id
                brushTileData &= ~Tileset.k_TileDataMask_BrushId;
                brushTileData |= tileData & Tileset.k_TileDataMask_BrushId; 
                return brushTileData;
            }
            return tileData;
        }

        #endregion
    }
}