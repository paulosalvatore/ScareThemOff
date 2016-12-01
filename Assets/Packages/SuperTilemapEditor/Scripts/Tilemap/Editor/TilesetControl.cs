using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;
using UnityEditorInternal;

namespace CreativeSpore.SuperTilemapEditor
{
    [Serializable]
    public class TilesetControl
    {
        public Tileset Tileset;

        public enum eViewMode
        {
            Tileset,
            TileView
        }
        private eViewMode m_viewMode;
        private eViewMode m_prevViewMode;

        private GUIStyle m_scrollStyle;
        private GUIStyle m_customBox;
        private bool m_displayBrushReordList = false;
        private Vector2 m_tilesScrollPos;
        private Vector2 m_tileScrollSpeed = Vector2.zero;
        private Vector2 m_brushesScrollPos;
        private int m_visibleTileCount = 1;
        private List<uint> m_visibleTileList = new List<uint>();
        private int m_tileViewRowLength = 1;
        private Rect m_rTileScrollSize;
        private Rect m_rTileScrollArea;
        private Rect m_rBrushScrollArea;
        private float m_lastTime;
        private float m_timeDt;
        private Vector2 m_lastTileScrollMousePos;
        private Color m_prevBgColor;
        private KeyValuePair<int, Rect> m_startDragTileIdxRect;
        private KeyValuePair<int, Rect> m_endDragTileIdxRect;
        private KeyValuePair<int, Rect> m_pointedTileIdxRect;
        private TilesetBrush m_selectBrushInInspector;

        private ReorderableList m_tileViewList;
        private ReorderableList m_brushList;

        private MouseDblClick m_dblClick = new MouseDblClick();
        public void Display()
        {
            Event e = Event.current;
            m_dblClick.Update();

            // This way a gui exception is avoided
            if( e.type == EventType.Layout && m_selectBrushInInspector != null)
            {
                Selection.activeObject = m_selectBrushInInspector;
                m_selectBrushInInspector = null;
            }

            if (m_lastTime == 0f)
            {
                m_lastTime = Time.realtimeSinceStartup;
            }
            m_timeDt = Time.realtimeSinceStartup - m_lastTime;
            m_lastTime = Time.realtimeSinceStartup;

            if (Tileset == null)
            {
                EditorGUILayout.HelpBox("There is no tileset selected", MessageType.Info);
                return;
            }
            else if (Tileset.AtlasTexture == null)
            {
                EditorGUILayout.HelpBox("There is no atlas texture set", MessageType.Info);
                return;
            }
            else if (Tileset.Tiles.Count == 0)
            {
                EditorGUILayout.HelpBox("There are no tiles to show in the current tileset", MessageType.Info);
                return;
            }

            if (m_scrollStyle == null)
            {
                m_scrollStyle = new GUIStyle("ScrollView");
            }

            if (m_customBox == null)
            {
                m_customBox = new GUIStyle("Box");
            }

            float visualTilePadding = 1;
            bool isLeftMouseReleased = e.type == EventType.MouseUp && e.button == 0;
            bool isRightMouseReleased = e.type == EventType.MouseUp && e.button == 1;
            bool isInsideTileScrollArea = e.isMouse && m_rTileScrollArea.Contains(e.mousePosition);
            bool isInsideBrushScrollArea = e.isMouse && m_rBrushScrollArea.Contains(e.mousePosition);

            // TileViews
            if (m_tileViewList == null || m_tileViewList.list != Tileset.TileViews)
            {
                if (e.type != EventType.Layout)
                {
                    m_tileViewList = TilesetEditor.CreateTileViewReorderableList( Tileset );
                    m_tileViewList.onSelectCallback += (ReorderableList list) =>
                    {
                        m_viewMode = eViewMode.TileView;
                        RemoveTileSelection();
                    };
                    m_tileViewList.onRemoveCallback += (ReorderableList list) =>
                    {
                        RemoveTileSelection();
                    };
                }
            }
            else
            {
                GUI.color = Color.cyan;
                GUILayout.BeginVertical(m_customBox);
                m_tileViewList.index = Mathf.Clamp(m_tileViewList.index, -1, Tileset.TileViews.Count - 1);
                m_tileViewList.DoLayoutList();
                Rect rList = GUILayoutUtility.GetLastRect();
                if (e.isMouse && !rList.Contains(e.mousePosition))
                {
                    m_tileViewList.ReleaseKeyboardFocus();
                }
                GUILayout.EndVertical();
                GUI.color = Color.white;
            }
            TileView tileView = m_tileViewList != null && m_tileViewList.index >= 0 ? Tileset.TileViews[m_tileViewList.index] : null;

            if (m_viewMode == eViewMode.Tileset)
            {
                Tileset.TileRowLength = Mathf.Clamp(EditorGUILayout.IntField("TileRowLength", Tileset.TileRowLength), 1, Tileset.Width);
            }

            m_viewMode = (eViewMode)EditorGUILayout.EnumPopup("View Mode", m_viewMode);
            if (tileView == null)
            {
                m_viewMode = eViewMode.Tileset;
            }
            if (m_viewMode != m_prevViewMode)
            {
                m_prevViewMode = m_viewMode;
                RemoveTileSelection();
            }

            // Draw Background Color Selector
            Tileset.BackgroundColor = EditorGUILayout.ColorField("Background Color", Tileset.BackgroundColor);
            if (m_prevBgColor != Tileset.BackgroundColor || m_scrollStyle.normal.background == null)
            {
                m_prevBgColor = Tileset.BackgroundColor;
                if (m_scrollStyle.normal.background == null) m_scrollStyle.normal.background = new Texture2D(1, 1) { hideFlags = HideFlags.DontSave };
                m_scrollStyle.normal.background.SetPixel(0, 0, Tileset.BackgroundColor);
                m_scrollStyle.normal.background.Apply();
            }
            //---

            // keep values safe
            m_tileViewRowLength = Mathf.Max(1, m_tileViewRowLength);

            float tileAreaWidth = m_tileViewRowLength * (Tileset.VisualTileSize.x + visualTilePadding);
            float tileAreaHeight = (Tileset.VisualTileSize.y + visualTilePadding) * (1 + (m_visibleTileCount - 1) / m_tileViewRowLength);
            m_tileViewRowLength = m_viewMode == eViewMode.TileView && tileView != null ? tileView.tileSelection.rowLength : Tileset.TileRowLength;

            m_tilesScrollPos = EditorGUILayout.BeginScrollView(m_tilesScrollPos, m_scrollStyle);
            {
                // Scroll Moving Drag
                if (e.type == EventType.MouseDrag && (e.button == 1 || e.button == 2))
                {
                    m_tilesScrollPos -= e.delta;
                }
                else
                {
                    DoAutoScroll();
                }

                if (e.isMouse)
                {
                    m_lastTileScrollMousePos = e.mousePosition;
                }
                if (Tileset.Tiles != null)
                {
                    GUILayoutUtility.GetRect(tileAreaWidth, tileAreaHeight);
                    m_visibleTileCount = 0;
                    List<uint> visibleTileList = new List<uint>();
                    int tileViewWidth = m_viewMode == eViewMode.Tileset ? Tileset.Width : tileView.tileSelection.rowLength;
                    int tileViewHeight = m_viewMode == eViewMode.Tileset ? Tileset.Height : ((tileView.tileSelection.selectionData.Count - 1) / tileView.tileSelection.rowLength) + 1;
                    int totalCount = ((((tileViewWidth - 1) / m_tileViewRowLength) + 1) * m_tileViewRowLength) * tileViewHeight;
                    for (int i = 0; i < totalCount; ++i)
                    {
                        int tileId = GetTileIdFromIdx(i, m_tileViewRowLength, tileViewWidth, tileViewHeight);
                        uint tileData = (uint)tileId;
                        if (m_viewMode == eViewMode.TileView && tileId != Tileset.k_TileId_Empty)
                        {
                            tileData = tileView.tileSelection.selectionData[tileId];
                            tileId = (int)(tileData & Tileset.k_TileDataMask_TileId);
                        }
                        Tile tile = tileId != Tileset.k_TileId_Empty && tileId < Tileset.Tiles.Count ? Tileset.Tiles[tileId] : null;
                        visibleTileList.Add(tileData);

                        int tx = m_visibleTileCount % m_tileViewRowLength;
                        int ty = m_visibleTileCount / m_tileViewRowLength;
                        Rect rVisualTile = new Rect(2 + tx * (Tileset.VisualTileSize.x + visualTilePadding), 2 + ty * (Tileset.VisualTileSize.y + visualTilePadding), Tileset.VisualTileSize.x, Tileset.VisualTileSize.y);

                        // Optimization, skipping not visible tiles
                        Rect rLocalVisualTile = rVisualTile; rLocalVisualTile.position -= m_tilesScrollPos;
                        if (!rLocalVisualTile.Overlaps(m_rTileScrollSize))
                        {
                            ; // Do Nothing
                        }
                        else
                        //---
                        {
                            // Draw Tile
                            if (tile == null)
                            {
                                HandlesEx.DrawRectWithOutline(rVisualTile, new Color(0f, 0f, 0f, 0.2f), new Color(0f, 0f, 0f, 0.2f));
                            }
                            else
                            {
                                HandlesEx.DrawRectWithOutline(rVisualTile, new Color(0f, 0f, 0f, 0.1f), new Color(0f, 0f, 0f, 0.1f));
                                TilesetEditor.DoGUIDrawTileFromTileData(rVisualTile, tileData, Tileset);                                
                            }

                            Rect rTileRect = new Rect(2 + tx * (Tileset.VisualTileSize.x + visualTilePadding), 2 + ty * (Tileset.VisualTileSize.y + visualTilePadding), (Tileset.VisualTileSize.x + visualTilePadding), (Tileset.VisualTileSize.y + visualTilePadding));
                            if (rVisualTile.Contains(e.mousePosition))
                            {
                                if (e.type == EventType.MouseDrag && e.button == 0)
                                    m_pointedTileIdxRect = new KeyValuePair<int, Rect>(m_visibleTileCount, rTileRect);
                                else if (e.type == EventType.MouseDown && e.button == 0)
                                    m_startDragTileIdxRect = m_pointedTileIdxRect = m_endDragTileIdxRect = new KeyValuePair<int, Rect>(m_visibleTileCount, rTileRect);
                                else if (e.type == EventType.MouseUp && e.button == 0)
                                {
                                    m_endDragTileIdxRect = new KeyValuePair<int, Rect>(m_visibleTileCount, rTileRect);
                                    DoSetTileSelection();
                                }
                            }

                            if ((isLeftMouseReleased || isRightMouseReleased) && isInsideTileScrollArea && rVisualTile.Contains(e.mousePosition)
                                && (m_startDragTileIdxRect.Key == m_endDragTileIdxRect.Key) // and there is not dragging selection
                                && m_rTileScrollSize.Contains(e.mousePosition - m_tilesScrollPos))// and it's inside the scroll area
                            {
                                Tileset.SelectedTileId = tileId;

                                //Give focus to SceneView to get key events
                                FocusSceneView();

                                if(isRightMouseReleased)
                                {
                                    TilePropertiesWindow.Show(Tileset);
                                }
                            }
                            else if (tile != null && Tileset.SelectedTileId == tileId)
                            {
                                HandlesEx.DrawRectWithOutline(rTileRect, new Color(0f, 0f, 0f, 0.1f), new Color(1f, 1f, 0f, 1f));
                            }                            
                        }

                        ++m_visibleTileCount;
                    }
                    m_visibleTileList = visibleTileList;

                    // Draw selection rect
                    if (m_startDragTileIdxRect.Key != m_pointedTileIdxRect.Key /*&& m_startDragTileIdxRect.Key == m_endDragTileIdxRect.Key*/)
                    {
                        Rect rSelection = new Rect(m_startDragTileIdxRect.Value.center, m_pointedTileIdxRect.Value.center - m_startDragTileIdxRect.Value.center);
                        rSelection.Set(Mathf.Min(rSelection.xMin, rSelection.xMax), Mathf.Min(rSelection.yMin, rSelection.yMax), Mathf.Abs(rSelection.width), Mathf.Abs(rSelection.height));
                        rSelection.xMin -= m_startDragTileIdxRect.Value.width / 2;
                        rSelection.xMax += m_startDragTileIdxRect.Value.width / 2;
                        rSelection.yMin -= m_startDragTileIdxRect.Value.height / 2;
                        rSelection.yMax += m_startDragTileIdxRect.Value.height / 2;
                        HandlesEx.DrawRectWithOutline(rSelection, new Color(0f, 0f, 0f, 0.1f), new Color(1f, 1f, 1f, 1f));
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            if (e.type == EventType.Repaint)
            {
                m_rTileScrollArea = GUILayoutUtility.GetLastRect();
                m_rTileScrollSize = m_rTileScrollArea;
                m_rTileScrollSize.position = Vector2.zero; // reset position to the Contains and Overlaps inside the tile scroll view without repositioning the position of local positions
                if (tileAreaWidth > m_rTileScrollSize.width)
                    m_rTileScrollSize.height -= GUI.skin.verticalScrollbar.fixedWidth;
                if (tileAreaHeight > m_rTileScrollSize.height)
                    m_rTileScrollSize.width -= GUI.skin.verticalScrollbar.fixedWidth;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Brush Palette", EditorStyles.boldLabel);
            m_displayBrushReordList = EditorUtils.DoToggleButton("Display List", m_displayBrushReordList);
            EditorGUILayout.EndHorizontal();

            int tileRowLength = (int)(m_rTileScrollSize.width / (Tileset.VisualTileSize.x + visualTilePadding));
            if (tileRowLength <= 0) tileRowLength = 1;
            float fBrushesScrollMaxHeight = Screen.height / 4;
            //commented because m_rTileScrollSize.width.height was changed to Screen.height;  fBrushesScrollMaxHeight -= fBrushesScrollMaxHeight % 2; // sometimes because size of tile scroll affects size of brush scroll, the height is dancing between +-1, so this is always taking the pair value
            float fBrushesScrollHeight = Mathf.Min(fBrushesScrollMaxHeight, 4 + (Tileset.VisualTileSize.y + visualTilePadding) * (1 + (Tileset.Brushes.Count / tileRowLength)));
            EditorGUILayout.BeginVertical(GUILayout.MinHeight(fBrushesScrollHeight));
            if (m_displayBrushReordList)
            {
                DisplayBrushReorderableList();
            }
            else
            {
                bool isRefreshBrushes = false;
                m_brushesScrollPos = EditorGUILayout.BeginScrollView(m_brushesScrollPos, m_scrollStyle);
                {
                    Rect rScrollView = new Rect(0, 0, m_rTileScrollSize.width, 0);
                    tileRowLength = Mathf.Clamp((int)rScrollView.width / (int)(Tileset.VisualTileSize.x + visualTilePadding), 1, tileRowLength);
                    if (Tileset.Brushes != null)
                    {
                        GUILayout.Space((Tileset.VisualTileSize.y + visualTilePadding) * (1 + (Tileset.Brushes.Count - 1) / tileRowLength));
                        for (int i = 0; i < Tileset.Brushes.Count; ++i)
                        {
                            Tileset.BrushContainer brushCont = Tileset.Brushes[i];
                            if (brushCont.BrushAsset == null)
                            {
                                isRefreshBrushes = true;
                                continue;
                            }

                            int tx = i % tileRowLength;
                            int ty = i / tileRowLength;
                            Rect rVisualTile = new Rect(2 + tx * (Tileset.VisualTileSize.x + visualTilePadding), 2 + ty * (Tileset.VisualTileSize.y + visualTilePadding), Tileset.VisualTileSize.x, Tileset.VisualTileSize.y);
                            //Fix Missing Tileset reference
                            if(brushCont.BrushAsset.Tileset == null)
                            {
                                Debug.LogWarning("Fixed missing tileset reference in brush " + brushCont.BrushAsset.name + "Id("+brushCont.Id+")");
                                brushCont.BrushAsset.Tileset = Tileset;
                            }
                            uint tileData = Tileset.k_TileData_Empty;
                            if (brushCont.BrushAsset.IsAnimated())
                            {
                                tileData = brushCont.BrushAsset.GetAnimTileData();
                            }
                            else
                            {
                                tileData = brushCont.BrushAsset.PreviewTileData();
                            }
                            TilesetEditor.DoGUIDrawTileFromTileData(rVisualTile, tileData, Tileset, brushCont.BrushAsset.GetAnimUV());
                            if ((isLeftMouseReleased || isRightMouseReleased || m_dblClick.IsDblClick) && isInsideBrushScrollArea && rVisualTile.Contains(Event.current.mousePosition))
                            {
                                Tileset.SelectedBrushId = brushCont.Id;
                                RemoveTileSelection();
                                if (m_dblClick.IsDblClick)
                                {
                                    EditorGUIUtility.PingObject(brushCont.BrushAsset);
                                    m_selectBrushInInspector = brushCont.BrushAsset;
                                }
                                if (isRightMouseReleased)
                                {
                                    TilePropertiesWindow.Show(Tileset);
                                }
                            }
                            else if (Tileset.SelectedBrushId == brushCont.Id)
                            {
                                Rect rSelection = new Rect(2 + tx * (Tileset.VisualTileSize.x + visualTilePadding), 2 + ty * (Tileset.VisualTileSize.y + visualTilePadding), (Tileset.VisualTileSize.x + visualTilePadding), (Tileset.VisualTileSize.y + visualTilePadding));
                                HandlesEx.DrawRectWithOutline(rSelection, new Color(0f, 0f, 0f, 0.1f), new Color(1f, 1f, 0f, 1f));
                            }
                        }
                    }

                    if (isRefreshBrushes)
                    {
                        Tileset.RemoveNullBrushes();
                    }
                }
                EditorGUILayout.EndScrollView();
                if (e.type == EventType.Repaint)
                {
                    m_rBrushScrollArea = GUILayoutUtility.GetLastRect();
                }
            }
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button("Import all brushes found"))
            {
                TilesetEditor.AddAllBrushesFoundInTheProject(Tileset);
                EditorUtility.SetDirty(Tileset);
            }
        }

        private void DisplayBrushReorderableList()
        {
            Event e = Event.current;
            if (m_brushList == null || m_brushList.list != Tileset.Brushes)
            {
                if (e.type != EventType.Layout)
                {
                    m_brushList = TilesetEditor.CreateBrushReorderableList(Tileset);
                    m_brushList.onSelectCallback += (ReorderableList list) =>
                    {
                        Tileset.BrushContainer brushCont = Tileset.Brushes[list.index];
                        Tileset.SelectedBrushId = brushCont.Id;
                        RemoveTileSelection();
                        if (m_dblClick.IsDblClick)
                        {
                            EditorGUIUtility.PingObject(brushCont.BrushAsset);
                            m_selectBrushInInspector = brushCont.BrushAsset;
                        }
                    };
                }
            }
            else
            {
                GUILayout.BeginVertical(m_customBox);
                m_brushList.index = Tileset.Brushes.FindIndex( x => x.Id == Tileset.SelectedBrushId);
                m_brushList.index = Mathf.Clamp(m_brushList.index, -1, Tileset.Brushes.Count - 1);
                m_brushList.elementHeight = Tileset.VisualTileSize.y + 10f;
                m_brushList.DoLayoutList();
                Rect rList = GUILayoutUtility.GetLastRect();
                if (e.isMouse && !rList.Contains(e.mousePosition))
                {
                    m_brushList.ReleaseKeyboardFocus();
                }
                GUILayout.EndVertical();
            }
        }

        private void FocusSceneView()
        {
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.Focus();
            }
            else if (SceneView.sceneViews.Count > 0)
            {
                ((SceneView)SceneView.sceneViews[0]).Focus();
            }
        }

        private void DoAutoScroll()
        {
            Event e = Event.current;
            if (m_rTileScrollSize.Contains(e.mousePosition - m_tilesScrollPos))
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    Vector2 mouseMoveDisp = e.mousePosition - m_lastTileScrollMousePos;
                    float autoScrollDist = 40;
                    float autoScrollSpeed = 500;
                    Vector2 mousePosition = e.mousePosition - m_tilesScrollPos;
                    float leftFactorX = mouseMoveDisp.x < 0f ? 1f - Mathf.Pow(Mathf.Clamp01(mousePosition.x / autoScrollDist), 2) : 0f;
                    float rightFactorX = mouseMoveDisp.x > 0f ? 1f - Mathf.Pow(Mathf.Clamp01((m_rTileScrollSize.width - mousePosition.x) / autoScrollDist), 2) : 0f;
                    float topFactorY = mouseMoveDisp.y < 0f ? 1f - Mathf.Pow(Mathf.Clamp01(mousePosition.y / autoScrollDist), 2) : 0f;
                    float bottomFactorY = mouseMoveDisp.y > 0f ? 1f - Mathf.Pow(Mathf.Clamp01((m_rTileScrollSize.height - mousePosition.y) / autoScrollDist), 2) : 0f;
                    m_tileScrollSpeed = autoScrollSpeed * new Vector2((-leftFactorX + rightFactorX), (-topFactorY + bottomFactorY));
                }
                else if (e.type == EventType.MouseUp)
                {
                    m_tileScrollSpeed = Vector2.zero;
                }
            }
            else if (e.isMouse)
            {
                m_tileScrollSpeed = Vector2.zero;
            }

            m_tilesScrollPos += m_timeDt * m_tileScrollSpeed;
        }

        private int GetTileIdFromIdx(int idx, int rowLength, int width, int height)
        {
            int cWxH = rowLength * height;
            int n = idx % cWxH;
            if (((idx / cWxH) * rowLength) + (idx % rowLength) >= width)
            {
                return Tileset.k_TileId_Empty;
            }
            return (n / rowLength) * width + idx % rowLength + (idx / cWxH) * rowLength;
        }

        private void RemoveTileSelection()
        {
            m_pointedTileIdxRect = m_startDragTileIdxRect = m_endDragTileIdxRect;
            Tileset.TileSelection = null;
        }

        private void DoSetTileSelection()
        {
            if (m_startDragTileIdxRect.Key != m_endDragTileIdxRect.Key)
            {
                int tx_start = Mathf.Min(m_startDragTileIdxRect.Key % m_tileViewRowLength, m_endDragTileIdxRect.Key % m_tileViewRowLength);
                int ty_start = Mathf.Min(m_startDragTileIdxRect.Key / m_tileViewRowLength, m_endDragTileIdxRect.Key / m_tileViewRowLength);
                int tx_end = Mathf.Max(m_startDragTileIdxRect.Key % m_tileViewRowLength, m_endDragTileIdxRect.Key % m_tileViewRowLength);
                int ty_end = Mathf.Max(m_startDragTileIdxRect.Key / m_tileViewRowLength, m_endDragTileIdxRect.Key / m_tileViewRowLength);
                List<uint> selectionData = new List<uint>();
                int tileIdx = 0;
                for (int ty = ty_end; ty >= ty_start; --ty) // NOTE: this goes from bottom to top to fit the tilemap coordinate system
                {
                    for (int tx = tx_start; tx <= tx_end; ++tx, ++tileIdx)
                    {
                        int visibleTileIdx = ty * m_tileViewRowLength + tx;
                        uint tileData = m_visibleTileList[visibleTileIdx];
                        selectionData.Add(tileData);
                    }
                }
                Tileset.TileSelection = new TileSelection(selectionData, tx_end - tx_start + 1);
                FocusSceneView(); //Give focus to SceneView to get key events
            }
        }
    }
}