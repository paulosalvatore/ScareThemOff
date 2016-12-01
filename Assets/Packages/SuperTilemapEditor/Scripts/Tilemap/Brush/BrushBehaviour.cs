using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CreativeSpore.SuperTilemapEditor
{

    [RequireComponent(typeof(Tilemap))]
    public class BrushBehaviour : MonoBehaviour
    {
        #region Singleton
        static BrushBehaviour s_instance;
        private static BrushBehaviour Instance
        {
            get
            {
                if (s_instance == null)
                {
                    BrushBehaviour[] brushes = FindObjectsOfType<BrushBehaviour>();
                    if (brushes.Length == 0)
                    {
                        GameObject obj = new GameObject("Brush");
                        s_instance = obj.AddComponent<BrushBehaviour>();
                    }
                    else
                    {
                        s_instance = brushes[0];
                        // Destroy the rest of brushes if any for any possible bug
                        if (brushes.Length > 1)
                        {
                            Debug.LogWarning("More than one brush found. Removing rest of brushes...");
                            for (int i = 1; i < brushes.Length; ++i)
                            {
                                DestroyImmediate(brushes[i]);
                            }
                        }
                    }
                }
                return s_instance;
            }
        }
        #endregion

        public float Alpha
        {
            get { return m_brushTilemap.Material.color.a; }
            set { m_brushTilemap.Material.color = new Color(m_brushTilemap.Material.color.r, m_brushTilemap.Material.color.g, m_brushTilemap.Material.color.b, value); }
        }

        public Tilemap BrushTilemap { get { return m_brushTilemap; } }
        public Vector2 Offset;
        public bool IsUndoEnabled = true;

        Tilemap m_brushTilemap;

        public static BrushBehaviour GetOrCreateBrush(Tilemap tilemap)
        {
            BrushBehaviour brush = Instance;
            if (brush.m_brushTilemap == null)
            {
                brush.m_brushTilemap = brush.GetComponent<Tilemap>();
                brush.m_brushTilemap.IsUndoEnabled = false;
                brush.m_brushTilemap.ColliderType = eColliderType.None;
                brush.Alpha = 0.7f;
            }
            bool needsRefresh = brush.m_brushTilemap.Tileset != tilemap.Tileset;
            brush.m_brushTilemap.Tileset = tilemap.Tileset;
            brush.m_brushTilemap.CellSize = tilemap.CellSize;
            brush.m_brushTilemap.SortingLayerID = tilemap.SortingLayerID;
            brush.m_brushTilemap.OrderInLayer = tilemap.OrderInLayer;
            //NOTE: dontsave brush give a lot of problems, like duplication of brushes FindObjectsOfType is not finding them
            //brush.m_brushTilemap.Material.hideFlags = HideFlags.DontSave; 
            //brush.gameObject.hideFlags = HideFlags.HideAndDontSave;
            //---
            brush.gameObject.hideFlags = HideFlags.HideInHierarchy;
            if (needsRefresh)
            {
                brush.m_brushTilemap.ClearMap();
                brush.m_brushTilemap.SetTileData(0, 0, Tileset.k_TileData_Empty); // setting an empty tile, map bounds increase a tile, needed to draw the brush border
                brush.m_brushTilemap.UpdateMesh();
            }

            return brush;
        }

        public static void SetVisible(bool isVisible)
        {
            if (s_instance != null)
            {
                s_instance.BrushTilemap.IsVisible = isVisible;
                for (int i = 0; i < s_instance.transform.childCount; ++i)
                {
                    s_instance.transform.GetChild(i).gameObject.SetActive(isVisible);
                }
            }
        }

        public static Tileset GetBrushTileset()
        {
            if (s_instance && s_instance.BrushTilemap)
            {
                return s_instance.BrushTilemap.Tileset;
            }
            return null;
        }

        public static TileSelection CreateTileSelection()
        {
            if (s_instance && s_instance.BrushTilemap && (s_instance.BrushTilemap.GridWidth * s_instance.BrushTilemap.GridHeight > 1) )
            {
                List<uint> selectionData = new List<uint>(s_instance.BrushTilemap.GridWidth * s_instance.BrushTilemap.GridHeight);
                for(int gy = 0; gy < s_instance.BrushTilemap.GridHeight; ++gy)
                {
                    for(int gx = 0; gx < s_instance.BrushTilemap.GridWidth; ++gx)
                    {
                        selectionData.Add(s_instance.BrushTilemap.GetTileData(gx, s_instance.BrushTilemap.GridHeight - gy - 1));
                    }
                }
                return new TileSelection(selectionData, s_instance.BrushTilemap.GridWidth);
            }
            return null;
        }

        void OnDestroy()
        {
            if (m_brushTilemap != null)
            {
                m_brushTilemap.ClearMap();
            }
        }

        public void FlipH(bool changeFlags = true)
        {
            m_brushTilemap.FlipH(changeFlags);
            m_brushTilemap.UpdateMesh();
        }

        public void FlipV(bool changeFlags = true)
        {
            m_brushTilemap.FlipV(changeFlags);
            m_brushTilemap.UpdateMesh();
        }

        public void Rot90(bool changeFlags = true)
        {

            int gridX = BrushUtil.GetGridX(-Offset, m_brushTilemap.CellSize);
            int gridY = BrushUtil.GetGridY(-Offset, m_brushTilemap.CellSize);

            Offset = -new Vector2(gridY * m_brushTilemap.CellSize.x, (m_brushTilemap.GridWidth - gridX - 1) * m_brushTilemap.CellSize.y);

            m_brushTilemap.Rot90(changeFlags);
            m_brushTilemap.UpdateMesh();
        }

        public void Rot90Back(bool changeFlags = true)
        {

            //NOTE: This is a fast way to rotate back 90º by rotating forward 3 times
            for (int i = 0; i < 3; ++i)
            {
                int gridX = BrushUtil.GetGridX(-Offset, m_brushTilemap.CellSize);
                int gridY = BrushUtil.GetGridY(-Offset, m_brushTilemap.CellSize);
                Offset = -new Vector2(gridY * m_brushTilemap.CellSize.x, (m_brushTilemap.GridWidth - gridX - 1) * m_brushTilemap.CellSize.y);
                m_brushTilemap.Rot90(changeFlags);
            }

            m_brushTilemap.UpdateMesh();
        }

        public void FloodFill(Tilemap tilemap, Vector2 localPos, uint tileData)
        {
            if (IsUndoEnabled)
            {
#if UNITY_EDITOR
                Undo.RecordObject(tilemap, Tilemap.k_UndoOpName + tilemap.name);
                Undo.RecordObjects(tilemap.GetComponentsInChildren<TilemapChunk>(), Tilemap.k_UndoOpName + tilemap.name);
#endif
            }
            tilemap.IsUndoEnabled = IsUndoEnabled;

            TilemapDrawingUtils.FloodFill(tilemap, localPos, tileData);
            tilemap.UpdateMesh();

            tilemap.IsUndoEnabled = false;
        }

        public void CopyRect(Tilemap tilemap, int startGridX, int startGridY, int endGridX, int endGridY)
        {
            for (int gridY = startGridY; gridY <= endGridY; ++gridY)
            {
                for (int gridX = startGridX; gridX <= endGridX; ++gridX)
                {
                    BrushTilemap.SetTileData(gridX - startGridX, gridY - startGridY, tilemap.GetTileData(gridX, gridY));
                }
            }
            BrushTilemap.UpdateMesh();
        }

        public void CutRect(Tilemap tilemap, int startGridX, int startGridY, int endGridX, int endGridY)
        {
            if (IsUndoEnabled)
            {
#if UNITY_EDITOR
                Undo.RecordObject(tilemap, Tilemap.k_UndoOpName + tilemap.name);
                Undo.RecordObjects(tilemap.GetComponentsInChildren<TilemapChunk>(), Tilemap.k_UndoOpName + tilemap.name);
#endif
            }
            tilemap.IsUndoEnabled = IsUndoEnabled;

            for (int gridY = startGridY; gridY <= endGridY; ++gridY)
            {
                for (int gridX = startGridX; gridX <= endGridX; ++gridX)
                {
                    BrushTilemap.SetTileData(gridX - startGridX, gridY - startGridY, tilemap.GetTileData(gridX, gridY));
                    tilemap.SetTileData(gridX, gridY, Tileset.k_TileData_Empty);
                }
            }
            BrushTilemap.UpdateMesh();
            tilemap.UpdateMesh();

            tilemap.IsUndoEnabled = false;
        }

        public void Paint(Tilemap tilemap, Vector2 localPos)
        {
            int minGridX = m_brushTilemap.MinGridX;
            int minGridY = m_brushTilemap.MinGridY;
            int maxGridX = m_brushTilemap.MaxGridX;
            int maxGridY = m_brushTilemap.MaxGridY;

            if (IsUndoEnabled)
            {
#if UNITY_EDITOR
                Undo.RecordObject(tilemap, Tilemap.k_UndoOpName + tilemap.name);
                Undo.RecordObjects(tilemap.GetComponentsInChildren<TilemapChunk>(), Tilemap.k_UndoOpName + tilemap.name);
#endif
            }
            tilemap.IsUndoEnabled = IsUndoEnabled;
            int dstGy = BrushUtil.GetGridY(localPos, tilemap.CellSize);
            for (int gridY = minGridY; gridY <= maxGridY; ++gridY, ++dstGy)
            {
                int dstGx = BrushUtil.GetGridX(localPos, tilemap.CellSize);
                for (int gridX = minGridX; gridX <= maxGridX; ++gridX, ++dstGx)
                {
                    uint tileData = m_brushTilemap.GetTileData(gridX, gridY);
                    if (tileData != Tileset.k_TileData_Empty)
                    {
                        tilemap.SetTileData(dstGx, dstGy, tileData);
                    }
                }
            }
            tilemap.UpdateMesh();
            tilemap.IsUndoEnabled = false;
        }

        public void Erase(Tilemap tilemap, Vector2 localPos)
        {
            int minGridX = m_brushTilemap.MinGridX;
            int minGridY = m_brushTilemap.MinGridY;
            int maxGridX = m_brushTilemap.MaxGridX;
            int maxGridY = m_brushTilemap.MaxGridY;

            if (IsUndoEnabled)
            {
#if UNITY_EDITOR
                Undo.RecordObject(tilemap, Tilemap.k_UndoOpName + tilemap.name);
                Undo.RecordObjects(tilemap.GetComponentsInChildren<TilemapChunk>(), Tilemap.k_UndoOpName + tilemap.name);
#endif
            }
            tilemap.IsUndoEnabled = IsUndoEnabled;
            int dstGy = BrushUtil.GetGridY(localPos, tilemap.CellSize);
            for (int gridY = minGridY; gridY <= maxGridY; ++gridY, ++dstGy)
            {
                int dstGx = BrushUtil.GetGridX(localPos, tilemap.CellSize);
                for (int gridX = minGridX; gridX <= maxGridX; ++gridX, ++dstGx)
                {
                    tilemap.SetTileData(dstGx, dstGy, Tileset.k_TileData_Empty);
                }
            }
            tilemap.UpdateMesh();
            tilemap.IsUndoEnabled = false;
        }
    }
}