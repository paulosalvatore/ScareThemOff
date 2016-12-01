using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace CreativeSpore.SuperTilemapEditor
{

    public enum eTileCollider
    {
        None = 0, //default value should be this one
        Full,
        Polygon
    }

    [Serializable]
    public struct TileColliderData
    {
        public Vector2[] vertices;
        public eTileCollider type;
        public TileColliderData Clone()
        {
            Vector2[] clonedVertices = new Vector2[this.vertices.Length];
            vertices.CopyTo(clonedVertices, 0);

            return new TileColliderData { vertices = clonedVertices, type = type };
        }

        public void FlipV()
        {
            for(int i = 0; i < vertices.Length; ++i)
            {
                vertices[i].x = 1f - vertices[i].x;
            }
            Array.Reverse(vertices);
        }

        public void FlipH()
        {
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i].y = 1f - vertices[i].y;
            }
            Array.Reverse(vertices);
        }

        public void Rot90()
        {
            for (int i = 0; i < vertices.Length; ++i)
            {
                float tempX = vertices[i].x;
                vertices[i].x = vertices[i].y;
                vertices[i].y = tempX;
                vertices[i].y = 1f - vertices[i].y;
            }
        }
    }

    [Serializable]
    public struct TilePrefabData
    {
        public enum eOffsetMode
        {
            Pixels,
            Units,
        };

        public GameObject prefab;
        public Vector3 offset;
        public eOffsetMode offsetMode;
        /// <summary>
        /// If the tile should be hidden or not if the prefab is attached
        /// </summary>
        public bool showTileWithPrefab;

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != this.GetType()) return false;

            TilePrefabData other = (TilePrefabData)obj;
            return (other.prefab == this.prefab) && (other.offset == this.offset) && (other.offsetMode == this.offsetMode);
        }

        public override int GetHashCode(){return base.GetHashCode();}
        public static bool operator ==(TilePrefabData c1, TilePrefabData c2){return c1.Equals(c2);}
        public static bool operator !=(TilePrefabData c1, TilePrefabData c2){return !c1.Equals(c2);}
    }

    [Serializable]
    public class Tile
    {
        public Rect uv;
        public TileColliderData collData;
        public ParameterContainer paramContainer = new ParameterContainer();
        public TilePrefabData prefabData;
    }

    [Serializable]
    public class TileSelection
    {
        public IList<uint> selectionData { get { return m_tileIds != null ? m_tileIds.AsReadOnly() : null; } }
        public int rowLength { get { return m_rowLength; } }

        [SerializeField]
        private int m_rowLength = 1;
        [SerializeField]
        private List<uint> m_tileIds; //NOTE: name remains for compatibility but now it contains tileData instead of only the id

        public TileSelection(List<uint> tileIds, int rowLength)
        {
            m_tileIds = tileIds != null ? tileIds : new List<uint>();
            m_rowLength = Mathf.Max(1, rowLength);
        }

        public TileSelection Clone()
        {
            List<uint> tileIds = new List<uint>(m_tileIds);
            int rowLength = m_rowLength;
            return new TileSelection(tileIds, rowLength);
        }

        public void FlipVertical()
        {
            List<uint> flipedTileIds = new List<uint>();
            int totalRows = 1 + (m_tileIds.Count - 1) / rowLength;
            for (int y = totalRows - 1; y >= 0; --y)
            {
                for (int x = 0; x < rowLength; ++x)
                {
                    int idx = y * rowLength + x;
                    flipedTileIds.Add(m_tileIds[idx]);
                }
            }
            m_tileIds = flipedTileIds;
        }
    }

    [Serializable]
    public class TileView
    {
        public TileView(string name, TileSelection tileSelection)
        {
            m_name = name;
            m_tileSelection = tileSelection;
        }

        public string name { get { return m_name; } }
        public TileSelection tileSelection { get { return m_tileSelection; } }

        [SerializeField]
        private string m_name;
        [SerializeField]
        private TileSelection m_tileSelection;
    }

    public class Tileset : ScriptableObject
    {
        public const int k_TileId_Empty = 0x0000FFFF; //NOTE: same value as k_TileDataMask_TileId
        public const int k_BrushId_Empty = 0; // brush id 0 is used for undefined brush
        public const uint k_TileData_Empty = 0xFFFFFFFF;
        // Tile Data Masks
        public const uint k_TileDataMask_TileId = 0x0000FFFF; // up to 256x256(65536 - 1) tiles (in a max. texture size of 8192x8192, min. tile size should be 32x32)
        public const uint k_TileDataMask_BrushId = 0x0FFF0000; // up to 4096 - 1 ( id 0 is used for undefined brush )
        public const uint k_TileDataMask_Flags = 0xF0000000; // Flags: (1bit)FlipX, (1bit)FlipY, (1bits)Rot90, (1 bit reserved)
        // Tile Data Flags
        public const uint k_TileFlag_FlipH = 0x80000000;
        public const uint k_TileFlag_FlipV = 0x40000000;
        public const uint k_TileFlag_Rot90 = 0x20000000;
        public const uint k_TileFlag_Updated = 0x10000000; // used by brushes to check when a tile should be updated or not

        public static int GetBrushIdFromTileData(uint tileData) { return tileData != k_TileData_Empty ? (int)((tileData & k_TileDataMask_BrushId) >> 16) : 0; }
        public static int GetTileIdFromTileData(uint tileData) { return (int)(tileData & k_TileDataMask_TileId); }
        /// <summary>
        /// Merge flags and keeps rotation coherence
        /// </summary>
        /// <returns></returns>
        public static uint GetMergedTileFlags(uint tileData, uint tileDataFlags)
        {
            tileDataFlags &= k_TileDataMask_Flags;
            tileData ^= tileDataFlags;
            if((tileData & k_TileFlag_Rot90) != 0)
            {
                tileData ^= (k_TileFlag_FlipH | k_TileFlag_FlipV);
            }
            return tileData;
        }

        #region Public Events
        public delegate void OnTileSelectedDelegate(Tileset source, int prevTileId, int newTileId);
        public OnTileSelectedDelegate OnTileSelected;

        public delegate void OnTileSelectionChangedDelegate(Tileset source);
        public OnTileSelectionChangedDelegate OnTileSelectionChanged;

        public delegate void OnBrushSelectedDelegate(Tileset source, int prevBrushId, int newBrushId);
        public OnBrushSelectedDelegate OnBrushSelected;
        #endregion

        #region Public Properties
        public Texture2D AtlasTexture;
        public Vector2 TilePxSize;
        public Vector2 SliceOffset;
        public Vector2 SlicePadding;
        public Color BackgroundColor = new Color32(205, 205, 205, 205);

        public Vector2 VisualTileSize = new Vector2(32, 32);
        public int VisualTilePadding = 1;
        public int TileRowLength = 8;        

        public int Width { get { return m_tilesetWidth; } }
        public int Height { get { return m_tilesetHeight; } }

        public bool GetGroupAutotiling(int groupA, int groupB) 
        { 
            return (m_brushGroupAutotilingMatrix[groupA] & (1u << groupB)) != 0; 
        }
        public void SetGroupAutotiling(int groupA, int groupB, bool value)
        {
            if (value)
            {
                m_brushGroupAutotilingMatrix[groupA] |= (1u << groupB);
                m_brushGroupAutotilingMatrix[groupB] |= (1u << groupA);
            }
            else
            {
                m_brushGroupAutotilingMatrix[groupA] &= ~(1u << groupB);
                m_brushGroupAutotilingMatrix[groupB] &= ~(1u << groupA);
            }
        }
        public string[] BrushGroupNames { get { return m_brushGroupNames; } }

        [Serializable]
        public struct BrushContainer
        {
            public int Id; // should be > 0
            public TilesetBrush BrushAsset;
        }

        public Tile SelectedTile { get { return SelectedTileId != k_TileId_Empty ? m_tiles[SelectedTileId] : null; } }
        public List<BrushContainer> Brushes { get { return m_brushes; } }
        public IList<Tile> Tiles { get { return m_tiles.AsReadOnly(); } }
        public float PixelsPerUnit { get { return m_pixelsPerUnit; } set { m_pixelsPerUnit = value; } }

        public Vector2 CalculateTileTexelSize()
        {
            return AtlasTexture != null ? Vector2.Scale(AtlasTexture.texelSize, TilePxSize) : Vector2.zero;
        }

        public List<TileView> TileViews { get { return m_tileViews; } }

        public int SelectedTileId
        {
            get
            {
                if (m_selectedTileId >= Tiles.Count || m_selectedTileId < 0)
                {
                    m_selectedTileId = k_TileId_Empty;
                }
                return m_selectedTileId;
            }

            set
            {
                int prevTileId = m_selectedTileId;
                m_selectedTileId = value;
                //if (m_selectedTileId != k_TileId_Empty) // commented to fix select empty tile from tilemap
                {
                    m_tileSelection = null;
                    m_selectedBrushId = k_BrushId_Empty;
                }
                //Debug.Log("SelectedTileId: " + SelectedTileId);
                if (OnTileSelected != null)
                {
                    OnTileSelected(this, prevTileId, m_selectedTileId);
                }
            }
        }

        public int SelectedBrushId
        {
            get { return m_selectedBrushId; }
            set
            {
                int prevBrushId = m_selectedBrushId;
                m_selectedBrushId = Mathf.Clamp(value, -1, m_tiles.Count - 1);
                m_selectedBrushId = (int)(m_selectedBrushId & k_TileDataMask_TileId); // convert -1 in k_TileId_Empty            

                //if (m_selectedBrushId != k_BrushId_Empty) // commented to fix select empty tile from tilemap
                {
                    m_selectedTileId = k_TileId_Empty;
                    m_tileSelection = null;
                }
                if (OnBrushSelected != null)
                {
                    OnBrushSelected(this, prevBrushId, m_selectedBrushId);
                }
            }
        }

        public TileSelection TileSelection
        {
            get
            {
                m_tileSelection = (m_tileSelection != null && m_tileSelection.selectionData != null && m_tileSelection.selectionData.Count > 0) ? m_tileSelection : null; //NOTE: sometimes m_tileSelection.tileIds has no tiles, even with the "set" check
                return m_tileSelection;
            }
            set
            {
                TileSelection prevValue = m_tileSelection;
                m_tileSelection = (value != null && value.selectionData != null && value.selectionData.Count > 0) ? value : null;
                if (m_tileSelection != null)
                {
                    m_selectedTileId = k_TileId_Empty;
                    m_selectedBrushId = k_BrushId_Empty;
                }
                if (prevValue != m_tileSelection && OnTileSelectionChanged != null)
                {
                    OnTileSelectionChanged(this);
                }
            }
        }

        #endregion

        #region Private Fields
        [SerializeField]
        private List<TileView> m_tileViews = new List<TileView>();
        [SerializeField]
        private int m_tilesetWidth = 0;
        [SerializeField]
        private int m_tilesetHeight = 0;
        [SerializeField]
        private List<BrushContainer> m_brushes = new List<BrushContainer>();
        [SerializeField]
        private List<Tile> m_tiles = new List<Tile>();
        [SerializeField]
        private float m_pixelsPerUnit = 100f;
        [SerializeField]
        private string[] m_brushGroupNames = Enumerable.Range(0, 32).Select( x => x == 0? "Default" : "").ToArray();
        [SerializeField]
        private uint[] m_brushGroupAutotilingMatrix = new uint[32];

        private int m_selectedTileId = k_TileId_Empty;
        private int m_selectedBrushId = -1;
        private TileSelection m_tileSelection = null;
        #endregion

        #region Public Methods
#if UNITY_EDITOR
        public void UpdateTilesetConfigFromAtlasImportSettings()
        {
            if (AtlasTexture != null)
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(AtlasTexture);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    UnityEditor.TextureImporter textureImporter = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
                    if (textureImporter != null)
                    {
                        m_pixelsPerUnit = textureImporter.spritePixelsPerUnit;
                        if(textureImporter.textureType == UnityEditor.TextureImporterType.Sprite)
                        {
                            if(textureImporter.spriteImportMode == UnityEditor.SpriteImportMode.Multiple)
                            {
                                List<Tile> tiles = new List<Tile>();
                                if (textureImporter.spritesheet.Length >= 2)
                                {
                                    UnityEditor.SpriteMetaData spr0 = textureImporter.spritesheet[0];
                                    UnityEditor.SpriteMetaData spr1 = textureImporter.spritesheet[1];
                                    TilePxSize = textureImporter.spritesheet[0].rect.size;
                                    SliceOffset = spr0.rect.position; SliceOffset.y = AtlasTexture.height - spr0.rect.y - spr0.rect.height;
                                    SlicePadding.x = spr1.rect.x - spr0.rect.xMax;                                    
                                }
                                //+++Ask before importing tiles
                                if (textureImporter.spritesheet.Length >= 2 && m_tiles.Count > 0)
                                {
                                    if( !UnityEditor.EditorUtility.DisplayDialog("Import Sprite Sheet?", "This texture atlas contain sliced sprites. Do you want to overwrite current tiles with these ones?", "Yes", "No") )
                                    {
                                        return;
                                    }
                                }
                                //---
                                m_tilesetHeight = 0;
                                foreach( UnityEditor.SpriteMetaData spriteData in textureImporter.spritesheet )
                                {
                                    Rect rUV = new Rect(Vector2.Scale(spriteData.rect.position, AtlasTexture.texelSize), Vector2.Scale(spriteData.rect.size, AtlasTexture.texelSize));
                                    tiles.Add(new Tile() { uv = rUV });
                                    if (tiles.Count >= 2)
                                    {
                                        if (tiles[tiles.Count - 2].uv.y != tiles[tiles.Count - 1].uv.y)
                                        {           
                                            if(m_tilesetHeight == 1)
                                            {
                                                m_tilesetWidth = tiles.Count - 1;
                                                SlicePadding.y = textureImporter.spritesheet[tiles.Count - 2].rect.y - textureImporter.spritesheet[tiles.Count - 1].rect.yMax;
                                            }
                                            ++m_tilesetHeight;
                                        }
                                    }
                                    else
                                    {
                                        ++m_tilesetHeight;
                                    }
                                }
                                TileRowLength = m_tilesetWidth;
                                //Copy data from previous tiles
                                if (m_tiles.Count == tiles.Count)
                                {
                                    for (int i = 0; i < m_tiles.Count; ++i)
                                    {
                                        tiles[i].collData = m_tiles[i].collData;
                                    }
                                }
                                m_tiles = tiles;
                            }
                        }
                    }
                }
            }
        }
#endif

        public void AddTileView(string name, TileSelection tileSelection, int idx = -1)
        {
            idx = idx >= 0 ? Mathf.Min(idx, m_tileViews.Count) : m_tileViews.Count;
            string viewName = name;
            int i = 1;
            while (m_tileViews.Exists(x => x.name == viewName))
            {
                viewName = name + " (" + i + ")";
                ++i;
            }
            m_tileViews.Insert(idx, new TileView(viewName, tileSelection));
        }

        public void RemoveTileView(string name)
        {
            m_tileViews.RemoveAll(x => x.name == name);
        }

        public void RenameTileView(string name, string newName)
        {
            int idx = m_tileViews.FindIndex(x => x.name == name);
            if (idx >= 0)
            {
                TileView tileView = m_tileViews[idx];
                RemoveTileView(name);
                AddTileView(newName, tileView.tileSelection, idx);
            }
        }

        public void RemoveAllTileViews()
        {
            m_tileViews.Clear();
        }

        public void SortTileViewsByName()
        {
            m_tileViews.Sort((TileView a, TileView b) => a.name.CompareTo(b.name));
        }

        public TileView FindTileView(string name)
        {
            return m_tileViews.Find(x => x.name == name);
        }

        public void RemoveNullBrushes()
        {
            m_brushes.RemoveAll(x => x.BrushAsset == null);
            m_brushCache.Clear();
        }

        public void AddBrush(TilesetBrush brush)
        {
            if (brush.Tileset == this)
            {
                if (!m_brushes.Exists(x => x.BrushAsset == brush))
                {
                    int id = m_brushes.Count > 0 ? m_brushes[m_brushes.Count - 1].Id : 1; //NOTE: id 0 is reserved for default brush                
                    int maxId = (int)(k_TileDataMask_BrushId >> 16);
                    if (m_brushes.Count >= maxId)
                    {
                        Debug.LogError(" Max number of brushes reached! " + maxId);
                    }
                    else
                    {
                        // find a not used id
                        while (m_brushes.Exists(x => x.Id == id))
                        {
                            ++id;
                            if (id > maxId)
                            {
                                id = 1;
                            }
                        }
                        m_brushes.Add(new BrushContainer() { Id = id, BrushAsset = brush });
                        m_brushCache.Clear();
                    }
                }
            }
            else
            {
                Debug.LogWarning("This brush " + brush.name + " has a different tileset and will not be added! ");
            }
        }

        Dictionary<int, TilesetBrush> m_brushCache = new Dictionary<int, TilesetBrush>();
        public TilesetBrush FindBrush(int brushId)
        {
            if (brushId == Tileset.k_BrushId_Empty)
            {
                return null;
            }
            TilesetBrush tileBrush = null;
            if (!m_brushCache.TryGetValue(brushId, out tileBrush))
            {
                tileBrush = m_brushes.FirstOrDefault(x => x.Id == brushId).BrushAsset;
                m_brushCache[brushId] = tileBrush;
                //Debug.Log(" Cache miss! " + tileBrush.name);
            }
            return tileBrush;
        }

        public int FindBrushId(string name)
        {
            return m_brushes.FirstOrDefault(x => x.BrushAsset.name == name).Id;
        }

        public void Slice()
        {
            List<Tile> tiles = new List<Tile>();
            if (AtlasTexture != null)
            {
                Vector2 tileTexelSize = CalculateTileTexelSize();
                float tileH = AtlasTexture.texelSize.y * TilePxSize.y;
                float uInc = AtlasTexture.texelSize.x * (TilePxSize.x + SlicePadding.x);
                float vInc = AtlasTexture.texelSize.y * (TilePxSize.y + SlicePadding.y);
                m_tilesetHeight = 0;
                if (uInc > 0 && vInc > 0)
                {
                    for (float v = SliceOffset.y * AtlasTexture.texelSize.y; v < 1f; v += vInc, ++m_tilesetHeight)
                    {
                        for (float u = SliceOffset.x * AtlasTexture.texelSize.x; u < 1f; u += uInc)
                        {
                            tiles.Add(new Tile() { uv = new Rect(new Vector2(u, 1f - v - tileH), tileTexelSize) });
                        }
                    }
                    m_tilesetWidth = tiles.Count / m_tilesetHeight;
                    TileRowLength = m_tilesetWidth;
                    //Copy data from previous tiles
                    if (m_tiles.Count == tiles.Count)
                    {
                        for (int i = 0; i < m_tiles.Count; ++i)
                        {
                            tiles[i].collData = m_tiles[i].collData;
                        }
                    }
                    m_tiles = tiles;
                }
                else
                {
                    Debug.LogWarning(" Error while slicing. There is something wrong with slicing parameters. uInc = " + uInc + "; vInc = " + vInc);
                }
            }
        }
        #endregion
    }
}