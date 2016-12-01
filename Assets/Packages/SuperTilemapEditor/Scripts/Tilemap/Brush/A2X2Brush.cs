using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.SuperTilemapEditor
{

    public class A2X2Brush : TilesetBrush
    {
        // '╔', '╗' | 2, 3,
        // '╚', '╝' | 0, 1,
        public uint[] TileIds = new uint[] // //NOTE: tileIds now contains tileData, not just tileIds
    {
        Tileset.k_TileData_Empty, // 3
        Tileset.k_TileData_Empty, // 6
        Tileset.k_TileData_Empty, // 9
        Tileset.k_TileData_Empty, // 12
    };

        #region IBrush

        public override uint PreviewTileData()
        {
            return TileIds[0];
        }

        public override uint Refresh(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            int brushId = (int)((tileData & Tileset.k_TileDataMask_BrushId) >> 16);
            //NOTE: Now, taking TileIds[0] by default, it means the tile collider will be taken from TileIds[0]
            return (tileData & Tileset.k_TileDataMask_Flags) | ((uint)(brushId << 16) | (TileIds[0] & Tileset.k_TileDataMask_TileId));
        }

        public override uint[] GetSubtiles(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            if (System.Array.IndexOf(TileIds, Tileset.k_TileData_Empty) >= 0)
            {
                return null;
            }

            int brushId = (int)((tileData & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_N = (int)((uint)(tilemap.GetTileData(gridX, gridY + 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_E = (int)((uint)(tilemap.GetTileData(gridX + 1, gridY) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_S = (int)((uint)(tilemap.GetTileData(gridX, gridY - 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_W = (int)((uint)(tilemap.GetTileData(gridX - 1, gridY) & Tileset.k_TileDataMask_BrushId) >> 16);

            // diagonals
            int brushId_NE = (int)((uint)(tilemap.GetTileData(gridX + 1, gridY + 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_SE = (int)((uint)(tilemap.GetTileData(gridX + 1, gridY - 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_SW = (int)((uint)(tilemap.GetTileData(gridX - 1, gridY - 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushId_NW = (int)((uint)(tilemap.GetTileData(gridX - 1, gridY + 1) & Tileset.k_TileDataMask_BrushId) >> 16);

            uint[] subTileData = new uint[4];
            subTileData[0] = (AutotileWith(brushId, brushId_SW) && AutotileWith(brushId, brushId_S) && AutotileWith(brushId, brushId_W)) ? TileIds[3] : TileIds[0];
            subTileData[1] = (AutotileWith(brushId, brushId_SE) && AutotileWith(brushId, brushId_S) && AutotileWith(brushId, brushId_E)) ? TileIds[2] : TileIds[1];
            subTileData[2] = (AutotileWith(brushId, brushId_NW) && AutotileWith(brushId, brushId_N) && AutotileWith(brushId, brushId_W)) ? TileIds[1] : TileIds[2];
            subTileData[3] = (AutotileWith(brushId, brushId_NE) && AutotileWith(brushId, brushId_N) && AutotileWith(brushId, brushId_E)) ? TileIds[0] : TileIds[3];

            return subTileData;
        }

        #endregion
    }
}