using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.SuperTilemapEditor
{
    [System.Flags]
    public enum eAutotilingMode
    {
        Self = 1,
        Other = 1 << 1,
        Group = 1 << 2,
    }

    public class TilesetBrush : ScriptableObject, IBrush
    {
        public Tileset Tileset;
        public ParameterContainer Params = new ParameterContainer();
        public int Group { get { return m_group; } }
        public eAutotilingMode AutotilingMode { get { return m_autotilingMode; } }

        [SerializeField]
        private int m_group = 0;
        
        [SerializeField]
        private eAutotilingMode m_autotilingMode = eAutotilingMode.Self;

        public bool AutotileWith(int selfBrushId, int otherBrushId)
        {
            if( 
                ((AutotilingMode & eAutotilingMode.Self) != 0 && selfBrushId == otherBrushId ) ||
                ((AutotilingMode & eAutotilingMode.Other) != 0 && otherBrushId != selfBrushId && otherBrushId != (Tileset.k_TileDataMask_BrushId >> 16))
            )
            {
                return true;
            }
            else if((AutotilingMode & eAutotilingMode.Group) != 0)
            {
                TilesetBrush brush = Tileset.FindBrush(otherBrushId);
                if(brush)
                {
                    return Tileset.GetGroupAutotiling(Group, brush.Group);
                }
            }
            return false;
        }

        protected static bool s_refreshingLinkedBrush = false; //avoid infinite loop
        public uint RefreshLinkedBrush(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            if (s_refreshingLinkedBrush) return tileData;

            int brushId = Tileset.GetBrushIdFromTileData(tileData);
            TilesetBrush brush = Tileset.FindBrush(brushId);
            if (brush)
            {
                s_refreshingLinkedBrush = true;
                tileData = brush.Refresh(tilemap, gridX, gridY, tileData);
                s_refreshingLinkedBrush = false;
            }
            return tileData;
        }

        #region IBrush

        public virtual uint PreviewTileData()
        {
            return Tileset.k_TileData_Empty;
        }

        public virtual uint OnPaint(TilemapChunk chunk, int chunkGx, int chunkGy, uint tileData)
        {
            return tileData;
        }

        public virtual void OnErase(TilemapChunk chunk, int chunkGx, int chunkGy, uint tileData)
        {
            ;
        }

        public virtual uint Refresh(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            return tileData;
        }

        public virtual bool IsAnimated()
        {
            return false;
        }

        public virtual Rect GetAnimUV()
        {
            int tileId = (int)(PreviewTileData() & Tileset.k_TileDataMask_TileId);
            return Tileset && tileId != Tileset.k_TileId_Empty ? Tileset.Tiles[tileId].uv : default(Rect);
        }

        public virtual uint GetAnimTileData()
        {
            return PreviewTileData();
        }

        public virtual uint[] GetSubtiles(Tilemap tilemap, int gridX, int gridY, uint tileData)
        {
            return null;
        }

        #endregion
    }
}