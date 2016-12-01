using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CreativeSpore.SuperTilemapEditor
{

    public class RoadBrush : TilesetBrush
    {
        // '°', '├', '═', '┤', | 0, 2, 10, 8,
        // '┬', '╔', '╦', '╗', | 4, 6, 14, 12,
        // '║', '╠', '╬', '╣', | 5, 7, 15, 13,
        // '┴', '╚', '╩', '╝', | 1, 3, 11, 9,
        public uint[] TileIds = Enumerable.Repeat(Tileset.k_TileData_Empty, 16).ToArray(); //NOTE: tileIds now contains tileData, not just tileIds

        #region IBrush

        public override uint PreviewTileData()
        {
            return TileIds[0];
        }

        public override uint Refresh(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            int brushId = (int)((tileData & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushIdTop = (int)((uint)(tilemap.GetTileData(gridX, gridY + 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushIdRight = (int)((uint)(tilemap.GetTileData(gridX + 1, gridY) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushIdBottom = (int)((uint)(tilemap.GetTileData(gridX, gridY - 1) & Tileset.k_TileDataMask_BrushId) >> 16);
            int brushIdLeft = (int)((uint)(tilemap.GetTileData(gridX - 1, gridY) & Tileset.k_TileDataMask_BrushId) >> 16);

            int idx = 0;
            if (AutotileWith(brushId, brushIdTop)) idx = 1;
            if (AutotileWith(brushId, brushIdRight)) idx |= 2;
            if (AutotileWith(brushId, brushIdBottom)) idx |= 4;
            if (AutotileWith(brushId, brushIdLeft)) idx |= 8;

            uint brushTileData = RefreshLinkedBrush(tilemap, gridX, gridY, TileIds[idx]);
            // overwrite flags
            brushTileData &= ~Tileset.k_TileDataMask_Flags;
            brushTileData |= TileIds[idx] & Tileset.k_TileDataMask_Flags;
            // overwrite brush id
            brushTileData &= ~Tileset.k_TileDataMask_BrushId;
            brushTileData |= tileData & Tileset.k_TileDataMask_BrushId;
            return brushTileData;
        }       

        #endregion
    }
}