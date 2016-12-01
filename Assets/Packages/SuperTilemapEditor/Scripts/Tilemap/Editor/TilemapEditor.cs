using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{

    /// <summary>
    /// Unity Event.current.clickCount is not always working as intended, so this class is used to check mouse double clicks safely
    /// </summary>
    internal class MouseDblClick
    {
        public bool IsDblClick { get { return m_isDblClick; } }
        float m_lastClickTime = 0f;
        bool m_isDblClick = false;

        public void Update()
        {
            Event e = Event.current;
            m_isDblClick = false;
            if (e.isMouse && e.type == EventType.MouseDown)
            {
                m_isDblClick = (Time.realtimeSinceStartup - m_lastClickTime) <= 0.2f;
                m_lastClickTime = Time.realtimeSinceStartup;
            }
        }
    }

    [CustomEditor(typeof(Tilemap))]
    public class TilemapEditor : Editor
    {
        [MenuItem("GameObject/SuperTilemapEditor/Tilemap", false, 10)]
        static void CreateTilemap()
        {
            GameObject obj = new GameObject("New Tilemap");
            obj.AddComponent<Tilemap>();
        }

        private class Styles
        {
            static Styles s_instance;
            public static Styles Instance 
            {
                get 
                {
                    if (s_instance == null)
                        s_instance = new Styles();
                    return s_instance;
                }
            }

            public GUIStyle toolbarBoxStyle = new GUIStyle()
            {
                normal = { textColor = Color.white },
                richText = true,
            };                        
        }

        private Tilemap m_tilemap;
        //private Editor m_matEditor;
        private TilesetControl m_tilesetCtrl;

        void OnEnable()
        {
            m_tilemap = (Tilemap)target;
            if (m_tilemap)
            {
                RegisterTilesetEvents(m_tilemap.Tileset);
            }
        }

        void OnDisable()
        {
            if (m_tilemap)
            {
                UnregisterTilesetEvents(m_tilemap.Tileset);
            }
            BrushBehaviour.SetVisible(false);
        }

        void RegisterTilesetEvents(Tileset tileset)
        {
            if (tileset != null)
            {
                UnregisterTilesetEvents(tileset);
                tileset.OnTileSelected += OnTileSelected;
                tileset.OnBrushSelected += OnBrushSelected;
                tileset.OnTileSelectionChanged += OnTileSelectionChanged;
            }
        }

        void UnregisterTilesetEvents(Tileset tileset)
        {
            if (tileset != null)
            {
                tileset.OnTileSelected -= OnTileSelected;
                tileset.OnBrushSelected -= OnBrushSelected;
                tileset.OnTileSelectionChanged -= OnTileSelectionChanged;
            }
        }

        void OnDestroy()
        {
            //        DestroyImmediate(m_matEditor);
        }

        private void OnTileSelected(Tileset source, int prevTileId, int newTileId)
        {
            ResetBrushMode();
            BrushBehaviour brush = BrushBehaviour.GetOrCreateBrush((Tilemap)target);
            brush.BrushTilemap.ClearMap();
            brush.BrushTilemap.SetTileData(0, 0, (uint)newTileId);
            brush.BrushTilemap.UpdateMesh();
            brush.Offset = Vector2.zero;
        }

        private void OnBrushSelected(Tileset source, int prevBrushId, int newBrushId)
        {
            ResetBrushMode();
            BrushBehaviour brush = BrushBehaviour.GetOrCreateBrush((Tilemap)target);
            brush.BrushTilemap.ClearMap();
            brush.BrushTilemap.SetTileData(0, 0, (uint)(newBrushId << 16) | Tileset.k_TileDataMask_TileId);
            brush.BrushTilemap.UpdateMesh();
            brush.Offset = Vector2.zero;
        }

        private void OnTileSelectionChanged(Tileset source)
        {
            ResetBrushMode();
            BrushBehaviour brush = BrushBehaviour.GetOrCreateBrush((Tilemap)target);
            brush.BrushTilemap.ClearMap();

            if (source.TileSelection != null)
            {
                for (int i = 0; i < source.TileSelection.selectionData.Count; ++i)
                {
                    int gx = i % source.TileSelection.rowLength;
                    int gy = i / source.TileSelection.rowLength;
                    brush.BrushTilemap.SetTileData(gx, gy, (uint)source.TileSelection.selectionData[i]);
                }
            }
            brush.BrushTilemap.UpdateMesh();
            brush.Offset = Vector2.zero;
        }

        private enum eBrushMode
        {
            Paint,
            Erase,
            Fill
        }
        static eBrushMode s_brushMode = eBrushMode.Paint;
        static bool s_brushFlipV = false;
        static bool s_brushFlipH = false;
        static bool s_brushRot90 = false;

        private eBrushMode GetBrushMode()
        {
            if (Event.current.shift) return eBrushMode.Erase;
            return s_brushMode;
        }

        public enum eEditMode
        {
            Paint,
            Renderer,
            Map,
            Collider,
        }
        public static eEditMode EditMode { get { return s_editMode; } }
        static eEditMode s_editMode = eEditMode.Paint;

        [SerializeField]
        private bool m_toggleMapBoundsEdit = false;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Tileset prevTileset = m_tilemap.Tileset;
            
            GUI.backgroundColor = Color.yellow;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            m_tilemap.Tileset = (Tileset)EditorGUILayout.ObjectField("Tileset", m_tilemap.Tileset, typeof(Tileset), false);
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            if (prevTileset != m_tilemap.Tileset)
            {
                UnregisterTilesetEvents(prevTileset);
                RegisterTilesetEvents(m_tilemap.Tileset);
            }

            if (m_tilemap.Tileset == null)
            {
                EditorGUILayout.HelpBox("There is no tileset selected", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                string[] editModeNames = System.Enum.GetNames(typeof(eEditMode));
                s_editMode = (eEditMode)GUILayout.Toolbar((int)s_editMode, editModeNames);
                if (s_editMode == eEditMode.Renderer)
                {
                    EditorGUI.BeginChangeCheck();
                    Material prevMaterial = m_tilemap.Material;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_material"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        m_tilemap.Refresh();
                        if (m_tilemap.Material != prevMaterial && !AssetDatabase.Contains(prevMaterial))
                        {
                            //avoid memory leak
                            DestroyImmediate(prevMaterial);
                        }
                    }
                    // Draw Material Control
                    /*
                    if (m_matEditor == null || EditorGUI.EndChangeCheck())
                    {
                        if (m_matEditor != null) DestroyImmediate(m_matEditor);
                        m_matEditor = MaterialEditor.CreateEditor(m_tilemap.Material);
                        m_matEditor.hideFlags = HideFlags.DontSave;
                    }
                    float savedLabelWidth = EditorGUIUtility.labelWidth;
                    m_matEditor.DrawHeader();
                    m_matEditor.OnInspectorGUI();
                    EditorGUIUtility.labelWidth = savedLabelWidth;
                    */
                    //---

                    m_tilemap.Material.color = EditorGUILayout.ColorField("Color", m_tilemap.Material.color);

                    //Pixel Snap
                    if (m_tilemap.Material.HasProperty("PixelSnap"))
                    {
                        EditorGUI.BeginChangeCheck();
                        bool isPixelSnapOn = EditorGUILayout.Toggle("Pixel Snap", m_tilemap.Material.IsKeywordEnabled("PIXELSNAP_ON"));
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_tilemap.Material.SetFloat("PixelSnap", isPixelSnapOn ? 1f : 0f);
                            if (isPixelSnapOn)
                            {
                                m_tilemap.Material.EnableKeyword("PIXELSNAP_ON");
                            }
                            else
                            {
                                m_tilemap.Material.DisableKeyword("PIXELSNAP_ON");
                            }
                        }
                    }

                    // Sorting Layer and Order in layer            
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sortingLayer"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_orderInLayer"));
                    serializedObject.FindProperty("m_orderInLayer").intValue = (serializedObject.FindProperty("m_orderInLayer").intValue << 16) >> 16; // convert from int32 to int16 keeping sign
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        m_tilemap.RefreshChunksSortingAttributes();
                        SceneView.RepaintAll();
                    }
                    //---            

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("InnerPadding"), new GUIContent("Inner Padding", "The size, in pixels, the tile UV will be stretched. Use this to fix pixel precision artifacts when tiles have no padding border in the atlas."));

                    m_tilemap.IsVisible = EditorGUILayout.Toggle("Visible", m_tilemap.IsVisible);
                }
                else if (s_editMode == eEditMode.Map)
                {
                    EditorGUILayout.Space();

                    if (GUILayout.Button("Refresh Map", GUILayout.MaxWidth(125)))
                    {
                        m_tilemap.Refresh(true, true, true, true);
                    }
                    if (GUILayout.Button("Clear Map", GUILayout.MaxWidth(125)))
                    {
                        if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to clear the map?\nThis action will remove all children objects under the tilemap", "Yes", "No"))
                        {
                            Undo.RegisterFullObjectHierarchyUndo(m_tilemap.gameObject, "Clear Map " + m_tilemap.name);
                            m_tilemap.IsUndoEnabled = true;
                            m_tilemap.ClearMap();
                            m_tilemap.IsUndoEnabled = false;
                        }
                    }
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_cellSize"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ShowGrid"), new GUIContent("Show Grid", "Show the tilemap grid."));
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Map Size (" + m_tilemap.GridWidth + "," + m_tilemap.GridHeight + ")");

                    //+++ Display Map Bounds
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Map Bounds (in tiles):", EditorStyles.boldLabel);
                    m_toggleMapBoundsEdit = EditorUtils.DoToggleIconButton("Edit Map Bounds", m_toggleMapBoundsEdit, EditorGUIUtility.IconContent("EditCollider"));

                    float savedLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 80;
                    EditorGUI.indentLevel += 2;

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_minGridX"), new GUIContent("Left"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_minGridY"), new GUIContent("Bottom"));
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_maxGridX"), new GUIContent("Right"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_maxGridY"), new GUIContent("Top"));
                    EditorGUILayout.EndVertical();
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        m_tilemap.RecalculateMapBounds();
                    }

                    EditorGUI.indentLevel -= 2;
                    EditorGUIUtility.labelWidth = savedLabelWidth;
                    EditorGUILayout.EndVertical();
                    //---

                    EditorGUILayout.Space();

                    m_tilemap.AllowPaintingOutOfBounds = EditorGUILayout.ToggleLeft("Allow Painting Out of Bounds", m_tilemap.AllowPaintingOutOfBounds);

                    EditorGUILayout.Space();

                    if (GUILayout.Button("Shrink to Visible Area", GUILayout.MaxWidth(150)))
                    {
                        m_tilemap.ShrinkMapBoundsToVisibleArea();
                    }
                }
                else if (s_editMode == eEditMode.Collider)
                {
                    EditorGUI.BeginChangeCheck();
                    {
                        //EditorGUILayout.PropertyField(serializedObject.FindProperty("ColliderType"));
                        EditorGUILayout.Space();

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField("Collider Type:", EditorStyles.boldLabel);
                        EditorGUI.indentLevel += 2;
                        SerializedProperty colliderTypeProperty = serializedObject.FindProperty("ColliderType");
                        string[] colliderTypeNames = new List<string>(System.Enum.GetNames(typeof(eColliderType)).Select(x => x.Replace('_', ' '))).ToArray();

                        colliderTypeProperty.intValue = GUILayout.SelectionGrid(colliderTypeProperty.intValue, colliderTypeNames, colliderTypeNames.Length);
                        EditorGUI.indentLevel -= 2;
                        EditorGUILayout.Space();
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.Space();

                        if (m_tilemap.ColliderType == eColliderType._3D)
                        {
                            SerializedProperty colliderDepthProperty = serializedObject.FindProperty("ColliderDepth");
                            EditorGUILayout.PropertyField(colliderDepthProperty);
                            colliderDepthProperty.floatValue = Mathf.Clamp(colliderDepthProperty.floatValue, Vector3.kEpsilon, Mathf.Max(colliderDepthProperty.floatValue));
                        }
                        else if (m_tilemap.ColliderType == eColliderType._2D)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("Collider2DType"));
                        }

                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_isTrigger"));
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        m_tilemap.Refresh(false, true);
                    }

                    EditorGUILayout.Space();

                    if (GUILayout.Button("Update Collider Mesh"))
                    {
                        m_tilemap.Refresh(false, true);
                    }
                }
                else if (s_editMode == eEditMode.Paint)
                {
                    if (m_tilemap.Tileset != null)
                    {
                        if (m_tilesetCtrl == null)
                        {
                            m_tilesetCtrl = new TilesetControl();
                        }
                        m_tilesetCtrl.Tileset = m_tilemap.Tileset;
                        m_tilesetCtrl.Display();
                    }
                }
            }
            EditorGUILayout.EndVertical();

            Repaint();
            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

        MouseDblClick m_dblClick = new MouseDblClick();
        public void OnSceneGUI()
        {
            m_dblClick.Update();
            Tilemap tilemap = (Tilemap)target;
            if (tilemap == null || tilemap.Tileset == null)
            {
                return;
            }

            BrushBehaviour.SetVisible(s_editMode == eEditMode.Paint);
            if(s_editMode == eEditMode.Paint)
            {
                DoPaintInspector();
            }
            else if(s_editMode == eEditMode.Map)
            {
                DoMapInspector();                
            }
        }

        Vector2 m_startDragging;
        Vector2 m_endDragging;
        bool m_isDragging = false;
        Vector2 m_localPaintPos;
        int m_mouseGridX;
        int m_mouseGridY;
        uint m_floodFillRestoredTileData = Tileset.k_TileData_Empty;
        private void DoPaintInspector()
        {
            Event e = Event.current;            

            Tilemap tilemap = (Tilemap)target;            

            if( DoToolBar() )
            {
                BrushBehaviour.SetVisible(false);
                SceneView.RepaintAll();
                return;
            }

            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);
            EventType currentEventType = Event.current.GetTypeForControl(controlID);
            bool skip = false;
            int saveControl = GUIUtility.hotControl;

            try
            {
                if (currentEventType == EventType.Layout) { skip = true; }
                else if (currentEventType == EventType.ScrollWheel) { skip = true; }

                if (tilemap.Tileset == null)
                {
                    return;
                }

                if (!skip)
                {
                    if (e.type == EventType.KeyDown)
                    {
                        if (e.keyCode == ShortcutKeys.k_FlipH)
                        {
                            BrushBehaviour.GetOrCreateBrush(tilemap).FlipH(!e.shift);
                            e.Use(); // Use key event
                        }
                        else if (e.keyCode == ShortcutKeys.k_FlipV)
                        {
                            BrushBehaviour.GetOrCreateBrush(tilemap).FlipV(!e.shift);
                            e.Use(); // Use key event
                        }
                        else if (e.keyCode == ShortcutKeys.k_Rot90)
                        {
                            BrushBehaviour.GetOrCreateBrush(tilemap).Rot90(!e.shift);
                            e.Use(); // Use key event
                        }
                        else if (e.keyCode == ShortcutKeys.k_Rot90Back)
                        {
                            BrushBehaviour.GetOrCreateBrush(tilemap).Rot90Back(!e.shift);
                            e.Use(); // Use key event
                        }
                    }

                    EditorGUIUtility.AddCursorRect(new Rect(0f, 0f, (float)Screen.width, (float)Screen.height), MouseCursor.Arrow);
                    GUIUtility.hotControl = controlID;
                    {
                        Plane chunkPlane = new Plane(tilemap.transform.forward, tilemap.transform.position);
                        Vector2 mousePos = Event.current.mousePosition; mousePos.y = Screen.height - mousePos.y;
                        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        float dist;
                        if (chunkPlane.Raycast(ray, out dist))
                        {
                            Rect rTile = new Rect(0, 0, m_tilemap.CellSize.x, m_tilemap.CellSize.y);
                            rTile.position = tilemap.transform.InverseTransformPoint(ray.GetPoint(dist));

                            Vector2 tilePos = rTile.position;
                            if (tilePos.x < 0) tilePos.x -= m_tilemap.CellSize.x;
                            if (tilePos.y < 0) tilePos.y -= m_tilemap.CellSize.y;
                            tilePos.x -= tilePos.x % m_tilemap.CellSize.x;
                            tilePos.y -= tilePos.y % m_tilemap.CellSize.y;
                            rTile.position = tilePos;


                            Vector2 startPos = new Vector2(Mathf.Min(m_startDragging.x, m_endDragging.x), Mathf.Min(m_startDragging.y, m_endDragging.y));
                            Vector2 endPos = new Vector2(Mathf.Max(m_startDragging.x, m_endDragging.x), Mathf.Max(m_startDragging.y, m_endDragging.y));
                            Vector2 selectionSnappedPos = BrushUtil.GetSnappedPosition(startPos, m_tilemap.CellSize);
                            Vector2 selectionSize = BrushUtil.GetSnappedPosition(endPos, m_tilemap.CellSize) - selectionSnappedPos + m_tilemap.CellSize;

                            BrushBehaviour brush = BrushBehaviour.GetOrCreateBrush(tilemap);
                            // Update brush transform
                            m_localPaintPos = (Vector2)tilemap.transform.InverseTransformPoint(ray.GetPoint(dist));
                            Vector2 brushSnappedPos = BrushUtil.GetSnappedPosition(brush.Offset + m_localPaintPos, m_tilemap.CellSize);
                            brush.transform.rotation = tilemap.transform.rotation;
                            brush.transform.localScale = tilemap.transform.lossyScale;
                            brush.transform.position = tilemap.transform.TransformPoint(new Vector3(brushSnappedPos.x, brushSnappedPos.y, -0.01f));
                            //---

                            int prevMouseGridX = m_mouseGridX;
                            int prevMouseGridY = m_mouseGridY;
                            if (e.isMouse)
                            {
                                m_mouseGridX = BrushUtil.GetGridX(m_localPaintPos, tilemap.CellSize);
                                m_mouseGridY = BrushUtil.GetGridY(m_localPaintPos, tilemap.CellSize);
                            }
                            bool isMouseGridChanged = prevMouseGridX != m_mouseGridX || prevMouseGridY != m_mouseGridY;

                            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag && isMouseGridChanged)
                            {
                                if (e.button == 0)
                                {
                                    if (m_dblClick.IsDblClick && brush.BrushTilemap.GridWidth == 1 && brush.BrushTilemap.GridHeight == 1)
                                    {
                                        // Restore previous tiledata modified by Paint, because before the double click, a single click is done before
                                        tilemap.SetTileData(brush.Offset + m_localPaintPos, m_floodFillRestoredTileData);
                                        brush.FloodFill(tilemap, brush.Offset + m_localPaintPos, brush.BrushTilemap.GetTileData(0, 0));
                                    }
                                    // Do a brush paint action
                                    else
                                    {
                                        switch (GetBrushMode())
                                        { 
                                            case eBrushMode.Paint:
                                                m_floodFillRestoredTileData = tilemap.GetTileData(m_mouseGridX, m_mouseGridY);
                                                brush.Paint(tilemap, brush.Offset + m_localPaintPos);
                                                break;
                                            case eBrushMode.Erase:
                                                brush.Erase(tilemap, brush.Offset + m_localPaintPos);
                                                break;
                                            case eBrushMode.Fill:
                                                brush.FloodFill(tilemap, brush.Offset + m_localPaintPos, brush.BrushTilemap.GetTileData(0, 0));
                                                break;
                                        }
                                    }
                                }
                                else if (e.button == 1)
                                {
                                    if (e.type == EventType.MouseDown)
                                    {
                                        m_isDragging = true;
                                        brush.BrushTilemap.ClearMap();
                                        m_startDragging = m_endDragging = m_localPaintPos;
                                    }
                                    else
                                    {
                                        m_endDragging = m_localPaintPos;
                                    }
                                }
                            }
                            else if (e.type == EventType.MouseUp)
                            {
                                if (e.button == 1) // right mouse button
                                {
                                    m_isDragging = false;
                                    ResetBrushMode();
                                    // Copy one tile
                                    if (selectionSize.x <= m_tilemap.CellSize.x && selectionSize.y <= m_tilemap.CellSize.y)
                                    {
                                        uint tileData = tilemap.GetTileData(m_localPaintPos);
                                        int brushId = Tileset.GetBrushIdFromTileData(tileData);
                                        int tileId = Tileset.GetTileIdFromTileData(tileData);
                                        
                                        // Select the copied tile in the tileset, alternating between the brush and the tile drawn by the brush
                                        if (brushId > 0 && tileId == tilemap.Tileset.SelectedTileId)
                                        {
                                            tilemap.Tileset.SelectedBrushId = brushId;
                                        }                                        
                                        else
                                        {
                                            tilemap.Tileset.SelectedTileId = tileId;
                                        }

                                        // Cut tile if key shift is pressed
                                        if (e.shift)
                                        {
                                            int startGridX = BrushUtil.GetGridX(startPos, m_tilemap.CellSize);
                                            int startGridY = BrushUtil.GetGridY(startPos, m_tilemap.CellSize);
                                            brush.CutRect(tilemap, startGridX, startGridY, startGridX, startGridY);
                                        }
                                        else
                                        {
                                            brush.BrushTilemap.SetTileData(0, 0, tileData);
                                        }
                                        brush.BrushTilemap.UpdateMesh();
                                        brush.Offset = Vector2.zero;
                                    }
                                    // copy a rect of tiles
                                    else
                                    {
                                        int startGridX = BrushUtil.GetGridX(startPos, m_tilemap.CellSize);
                                        int startGridY = BrushUtil.GetGridY(startPos, m_tilemap.CellSize);
                                        int endGridX = BrushUtil.GetGridX(endPos, m_tilemap.CellSize);
                                        int endGridY = BrushUtil.GetGridY(endPos, m_tilemap.CellSize);

                                        // Cut tile if key shift is pressed
                                        if (e.shift)
                                        {
                                            brush.CutRect(tilemap, startGridX, startGridY, endGridX, endGridY);
                                        }
                                        else
                                        {
                                            brush.CopyRect(tilemap, startGridX, startGridY, endGridX, endGridY);
                                        }
                                        brush.Offset.x = m_endDragging.x > m_startDragging.x ? -(endGridX - startGridX) * tilemap.CellSize.x : 0f;
                                        brush.Offset.y = m_endDragging.y > m_startDragging.y ? -(endGridY - startGridY) * tilemap.CellSize.y : 0f;

                                    }
                                }
                            }

                            if (m_isDragging)
                            {
                                Rect rGizmo = new Rect(selectionSnappedPos, selectionSize);
                                HandlesEx.DrawRectWithOutline(tilemap.transform, rGizmo, new Color(), Color.white);
                            }
                            else // Draw brush border
                            {
                                Rect rBound = new Rect(brush.BrushTilemap.MapBounds.min, brush.BrushTilemap.MapBounds.size);
                                Color fillColor;
                                switch (GetBrushMode())
                                {
                                    case eBrushMode.Paint:
                                        fillColor = new Color(0, 0, 0, 0);
                                        break;
                                    case eBrushMode.Erase:
                                        fillColor = new Color(1f, 0f, 0f, 0.2f);
                                        break;
                                    case eBrushMode.Fill:
                                        fillColor = new Color(1f, 1f, 0f, 0.2f);
                                        break;
                                    default:
                                        fillColor = new Color(0, 0, 0, 0);
                                        break;
                                }
                                HandlesEx.DrawRectWithOutline(brush.transform, rBound, fillColor, new Color(1, 1, 1, 0.2f));
                            }
                        }
                    }

                    if (currentEventType == EventType.MouseDrag && Event.current.button < 2) // 2 is for central mouse button
                    {
                        // avoid dragging the map
                        Event.current.Use();
                    }
                }
            }
            // Avoid loosing the hotControl because of a triggered exception
            catch(System.Exception ex)
            {
                Debug.LogException(ex);
            }

            SceneView.RepaintAll();
            GUIUtility.hotControl = saveControl;
            serializedObject.ApplyModifiedProperties();
        }

        private void DoMapInspector()
        {
            Tilemap tilemap = (Tilemap)target;

            if (m_toggleMapBoundsEdit)
            {
                EditorGUI.BeginChangeCheck();
                Handles.color = Color.green;
                Vector3 vMinX = Handles.FreeMoveHandle(tilemap.transform.TransformPoint(new Vector2(tilemap.MinGridX * tilemap.CellSize.x, tilemap.MapBounds.center.y)), Quaternion.identity, 0.1f * HandleUtility.GetHandleSize(tilemap.transform.position), Vector3.zero, Handles.CubeCap);
                Vector3 vMaxX = Handles.FreeMoveHandle(tilemap.transform.TransformPoint(new Vector2((tilemap.MaxGridX + 1f) * tilemap.CellSize.x, tilemap.MapBounds.center.y)), Quaternion.identity, 0.1f * HandleUtility.GetHandleSize(tilemap.transform.position), Vector3.zero, Handles.CubeCap);
                Vector3 vMinY = Handles.FreeMoveHandle(tilemap.transform.TransformPoint(new Vector2(tilemap.MapBounds.center.x, tilemap.MinGridY * tilemap.CellSize.y)), Quaternion.identity, 0.1f * HandleUtility.GetHandleSize(tilemap.transform.position), Vector3.zero, Handles.CubeCap);
                Vector3 vMaxY = Handles.FreeMoveHandle(tilemap.transform.TransformPoint(new Vector2(tilemap.MapBounds.center.x, (tilemap.MaxGridY + 1f) * tilemap.CellSize.y)), Quaternion.identity, 0.1f * HandleUtility.GetHandleSize(tilemap.transform.position), Vector3.zero, Handles.CubeCap);
                Handles.color = Color.white;
                serializedObject.FindProperty("m_minGridX").intValue = Mathf.RoundToInt(tilemap.transform.InverseTransformPoint(vMinX).x / tilemap.CellSize.x);
                serializedObject.FindProperty("m_maxGridX").intValue = Mathf.RoundToInt(tilemap.transform.InverseTransformPoint(vMaxX).x / tilemap.CellSize.x - 1f);
                serializedObject.FindProperty("m_minGridY").intValue = Mathf.RoundToInt(tilemap.transform.InverseTransformPoint(vMinY).y / tilemap.CellSize.y);
                serializedObject.FindProperty("m_maxGridY").intValue = Mathf.RoundToInt(tilemap.transform.InverseTransformPoint(vMaxY).y / tilemap.CellSize.y - 1f);
                if(EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    tilemap.RecalculateMapBounds();
                }
            }
        }

        void ResetBrushMode()
        {
            s_brushMode = eBrushMode.Paint;
            s_brushFlipH = s_brushFlipV = s_brushRot90 = false;
        }

        static Color s_toolbarBoxBgColor = new Color(0f, 0f, .4f, 0.4f);
        static Color s_toolbarBoxOutlineColor = new Color(.25f, .25f, 1f, 0.70f);
        bool DoToolBar()
        {
            bool isMouseInsideToolbar = false;
            Tilemap tilemap = (Tilemap)target;
            GUIContent brushCoords = new GUIContent("<b> Brush Pos: (" + m_mouseGridX + "," + m_mouseGridY + ")</b>");
            GUIContent selectedTileOrBrushId = null;
            if (tilemap.Tileset.SelectedTileId != Tileset.k_TileId_Empty)
                selectedTileOrBrushId = new GUIContent("<b> Selected Tile Id: " + tilemap.Tileset.SelectedTileId.ToString() + "</b>");
            else if (tilemap.Tileset.SelectedBrushId != Tileset.k_BrushId_Empty)
                selectedTileOrBrushId = new GUIContent("<b> Selected Brush Id: " + tilemap.Tileset.SelectedBrushId.ToString() + "</b>");
            else
                selectedTileOrBrushId = new GUIContent("<b> Empty tile selected</b>");

            Rect rTools = new Rect(4f, 4f, Mathf.Max(Mathf.Max(Styles.Instance.toolbarBoxStyle.CalcSize(brushCoords).x, Styles.Instance.toolbarBoxStyle.CalcSize(selectedTileOrBrushId).x) + 4f, 180f), 54f);
            
            Handles.BeginGUI();            
            GUILayout.BeginArea(rTools);
            HandlesEx.DrawRectWithOutline(new Rect(Vector2.zero, rTools.size), s_toolbarBoxBgColor, s_toolbarBoxOutlineColor);

            GUILayout.Space(2f);
            GUILayout.Label(brushCoords, Styles.Instance.toolbarBoxStyle);
            if (selectedTileOrBrushId != null)
            {
                GUILayout.Label(selectedTileOrBrushId, Styles.Instance.toolbarBoxStyle);
            }
            GUILayout.Label("<b> F1 - Display Help</b>", Styles.Instance.toolbarBoxStyle);
            GUILayout.Label("<b> F5 - Refresh Tilemap</b>", Styles.Instance.toolbarBoxStyle);
            GUILayout.EndArea();

            // Display ToolBar
            int buttonNb = System.Enum.GetValues(typeof(ToolIcons.eToolIcon)).Length;
            Rect rToolBar = new Rect(rTools.xMax + 4f, rTools.y, buttonNb * 32f, 32f);
            isMouseInsideToolbar = rToolBar.Contains(Event.current.mousePosition);
            GUILayout.BeginArea(rToolBar);
            HandlesEx.DrawRectWithOutline(new Rect(Vector2.zero, rToolBar.size), s_toolbarBoxBgColor, s_toolbarBoxOutlineColor);
            GUILayout.BeginHorizontal();

            int buttonPadding = 4;
            Rect rToolBtn = new Rect(buttonPadding, buttonPadding, rToolBar.size.y - 2 * buttonPadding, rToolBar.size.y - 2 * buttonPadding);
            foreach (ToolIcons.eToolIcon toolIcon in System.Enum.GetValues(typeof(ToolIcons.eToolIcon)))
            {
                _DoToolbarButton(rToolBtn, toolIcon);                
                rToolBtn.x = rToolBtn.xMax + 2*buttonPadding;
            }            
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            //---

            Handles.EndGUI();

            if(m_displayHelpBox)
            {
                DisplayHelpBox();
            }
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.F1)
                    m_displayHelpBox = !m_displayHelpBox;
                else if (Event.current.keyCode == KeyCode.F5)
                    m_tilemap.Refresh(true, true, true, true);
            }

            return isMouseInsideToolbar;
        }

        private void _DoToolbarButton(Rect rToolBtn, ToolIcons.eToolIcon toolIcon)
        {
            BrushBehaviour brush = BrushBehaviour.GetOrCreateBrush((Tilemap)target);
            int iconPadding = 6;
            Rect rToolIcon = new Rect(rToolBtn.x + iconPadding, rToolBtn.y + iconPadding, rToolBtn.size.y - 2 * iconPadding, rToolBtn.size.y - 2 * iconPadding);
            Color activeColor = new Color(1f, 1f, 1f, 0.8f);
            Color disableColor = new Color(1f, 1f, 1f, 0.4f);
            switch(toolIcon)
            {
                case ToolIcons.eToolIcon.Pencil:
                    GUI.color = GetBrushMode() == eBrushMode.Paint ? activeColor : disableColor;
                    if( GUI.Button(rToolBtn, new GUIContent("", "Paint")) )
                    {
                        s_brushMode = eBrushMode.Paint;
                    }
                    break;
                case ToolIcons.eToolIcon.Erase:
                    GUI.color = GetBrushMode() == eBrushMode.Erase ? activeColor : disableColor;
                    if (GUI.Button(rToolBtn, new GUIContent("", "Erase (Hold Shift)")))
                    {
                        s_brushMode = eBrushMode.Erase;
                    }
                    break;
                case ToolIcons.eToolIcon.Fill:
                    GUI.color = GetBrushMode() == eBrushMode.Fill ? activeColor : disableColor;
                    if (GUI.Button(rToolBtn, new GUIContent("", "Fill (Double click)")))
                    {
                        s_brushMode = eBrushMode.Fill;
                    }
                    break;
                case ToolIcons.eToolIcon.FlipV:
                    GUI.color = s_brushFlipV ? activeColor : disableColor;
                    if (GUI.Button(rToolBtn, new GUIContent("", "Flip Vertical ("+ShortcutKeys.k_FlipV+")")))
                    {
                        brush.FlipV();
                        s_brushFlipV = !s_brushFlipV;
                    }
                    break;
                case ToolIcons.eToolIcon.FlipH:
                    GUI.color = s_brushFlipH ? activeColor : disableColor;
                    if (GUI.Button(rToolBtn, new GUIContent("", "Flip Horizontal ("+ShortcutKeys.k_FlipH+")")))
                    {
                        brush.FlipH();
                        s_brushFlipH = !s_brushFlipH;
                    }
                    break;
                case ToolIcons.eToolIcon.Rot90:
                    GUI.color = s_brushRot90 ? activeColor : disableColor;
                    if (GUI.Button(rToolBtn, new GUIContent("", "Rotate 90 clockwise (" + ShortcutKeys.k_Rot90 + "); anticlockwise (" + ShortcutKeys.k_Rot90Back + ")")))
                    {
                        if (!s_brushRot90)
                            brush.Rot90();
                        else 
                            brush.Rot90Back();
                        s_brushRot90 = !s_brushRot90;
                    }
                    break;
                case ToolIcons.eToolIcon.Info:
                    GUI.color = m_displayHelpBox ? activeColor : disableColor;
                    if (GUI.Button(rToolBtn, new GUIContent("", " Display Help (F1)")))
                    {
                        m_displayHelpBox = !m_displayHelpBox;
                    }
                    break;
                case ToolIcons.eToolIcon.Refresh:
                    GUI.color = m_displayHelpBox ? activeColor : disableColor;
                    if (GUI.Button(rToolBtn, new GUIContent("", " Refresh Tilemap (F5)")))
                    {
                        m_tilemap.Refresh(true, true, true, true);
                    }
                    break;
            }            
            GUI.color = Color.white;
            GUI.DrawTexture(rToolIcon, ToolIcons.GetToolTexture(toolIcon));            
        }

        private bool m_displayHelpBox = false;
        void DisplayHelpBox()
        {
            string sHelp =
                "\n" +
                " - <b>Drag:</b>\t Middle mouse button\n" +
                " - <b>Paint:</b>\t Left mouse button\n" +
                " - <b>Erase:</b>\t Shift + Left mouse button\n" +
                " - <b>Fill:</b>\t Double Click\n\n" +
                " - <b>Copy</b> tiles by dragging and holding right mouse button\n\n" +
                " - <b>Cut</b> copy while holding Shift key\n\n" +
                " - <b>Rotating and flipping:</b>\n" +
                "   * <b>Rotate</b> ±90º by using <b>comma ','</b> and <b>period '.'</b>\n" +
                "   * <b>Vertical Flip</b> by pressing X\n" +
                "   * <b>Horizontal Flip</b> by pressing Y\n" +
                "   * <i>Hold shift to only rotate or flip tile positions</i>\n" +
                "\n - <b>Use Ctrl-Z/Ctrl-Y</b> to Undo/Redo changes\n";
            GUIContent helpContent = new GUIContent(sHelp);
            Handles.BeginGUI();
            Rect rHelpBox = new Rect(new Vector2(2f, 64f), Styles.Instance.toolbarBoxStyle.CalcSize(helpContent));
            GUILayout.BeginArea(rHelpBox);
            HandlesEx.DrawRectWithOutline(new Rect(Vector2.zero, rHelpBox.size), s_toolbarBoxBgColor, s_toolbarBoxOutlineColor);
            GUILayout.Label(sHelp, Styles.Instance.toolbarBoxStyle);
            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}