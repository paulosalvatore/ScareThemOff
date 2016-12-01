using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using System;

namespace CreativeSpore.SuperTilemapEditor
{
    [CustomEditor(typeof(Tileset))]
    public class TilesetEditor : Editor
    {
        [MenuItem("Assets/Create/SuperTilemapEditor/Tileset")]
        public static Tileset CreateTileset()
        {
            return EditorUtils.CreateAssetInSelectedDirectory<Tileset>();
        }

        private TilesetControl m_tilesetCtrl = new TilesetControl();

        protected SerializedProperty m_brushGroupNames;
        private ReorderableList m_groupsList;
        public virtual void OnEnable()
        {
            this.m_brushGroupNames = base.serializedObject.FindProperty("m_brushGroupNames");
            if (this.m_groupsList == null)
			{
                this.m_groupsList = new ReorderableList(base.serializedObject, this.m_brushGroupNames, false, false, false, false);
				this.m_groupsList.elementHeight = EditorGUIUtility.singleLineHeight + 2f;
				this.m_groupsList.headerHeight = 3f;
                this.m_groupsList.drawElementCallback = (Rect rect, int index, bool selected, bool focused) =>
                {
                    rect.height -= 2f;
                    string stringValue = this.m_brushGroupNames.GetArrayElementAtIndex(index).stringValue;
                    string text;
                    GUI.enabled = true;// index > 0;
                    text = EditorGUI.TextField(rect, " Brush Group " + index, stringValue);
                    GUI.enabled = true;
                    if (text != stringValue)
                    {
                        this.m_brushGroupNames.GetArrayElementAtIndex(index).stringValue = text;
                    }
                };
			}
        }

        static bool s_gridFoldout = true;
        static bool s_tilePaletteFoldout = true;
        static bool s_brushGroupsFoldout = false;
        static bool s_brushAutotilingMaskFoldout = false;
        static Vector2 s_brushGroupMatrixScrollPos = Vector2.zero;
        public override void OnInspectorGUI()
        {
            Tileset tileset = (Tileset)target;
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AtlasTexture"));
            if(EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                tileset.UpdateTilesetConfigFromAtlasImportSettings();
            }
            if (tileset.AtlasTexture == null)
            {
                EditorGUILayout.HelpBox("Select an atlas texture for the tileset", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_pixelsPerUnit"));
                s_gridFoldout = EditorGUILayout.Foldout(s_gridFoldout, "Grid");
                if (s_gridFoldout)
                {
                    tileset.TilePxSize = _GetPositiveIntVect2(EditorGUILayout.Vector2Field("Pixel Size", tileset.TilePxSize));
                    tileset.SliceOffset = _GetPositiveIntVect2(EditorGUILayout.Vector2Field("Offset", tileset.SliceOffset));
                    tileset.SlicePadding = _GetPositiveIntVect2(EditorGUILayout.Vector2Field("Padding", tileset.SlicePadding));

                    if (GUILayout.Button("Slice Atlas"))
                    {
                        tileset.Slice();
                    }
                }

                s_tilePaletteFoldout = EditorGUILayout.Foldout(s_tilePaletteFoldout, "Tile Palette");
                if (s_tilePaletteFoldout)
                {
                    m_tilesetCtrl.Tileset = tileset;
                    m_tilesetCtrl.Display();
                    Repaint();
                }

                s_brushGroupsFoldout = EditorGUILayout.Foldout(s_brushGroupsFoldout, "Brush Groups");
                if (s_brushGroupsFoldout)
                {
                    m_groupsList.DoLayoutList();
                }

                GroupMatrixGUI.DoGUI("Group Autotiling Mask", tileset.BrushGroupNames, ref s_brushAutotilingMaskFoldout, ref s_brushGroupMatrixScrollPos, GetValue, SetValue);
            }

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        private bool GetValue(int groupA, int groupB)
        {
            Tileset tileset = (Tileset)target;
            return tileset.GetGroupAutotiling(groupA, groupB);
        }
        private void SetValue(int groupA, int groupB, bool val)
        {
            Tileset tileset = (Tileset)target;
            tileset.SetGroupAutotiling(groupA, groupB, val);
        }

        private Vector2 _GetPositiveIntVect2(Vector2 v)
        {
            return new Vector2(Mathf.Max(0, (int)v.x), Mathf.Max(0, (int)v.y));
        }

        public static Tileset GetSelectedTileset()
        {
            if (Selection.activeObject is Tileset)
            {
                return Selection.activeObject as Tileset;
            }
            else if (Selection.activeObject is TilesetBrush)
            {
                return (Selection.activeObject as TilesetBrush).Tileset;
            }
            else if (Selection.activeObject is GameObject)
            {
                Tilemap tilemap = (Selection.activeObject as GameObject).GetComponent<Tilemap>();
                if (tilemap == null)
                {
                    TilemapGroup tilemapGroup = (Selection.activeObject as GameObject).GetComponent<TilemapGroup>();
                    if (tilemapGroup != null)
                    {
                        tilemap = tilemapGroup.SelectedTilemap;
                    }
                }
                if (tilemap != null)
                {
                    return tilemap.Tileset;
                }
            }
            return null;
        }

        public static void AddAllBrushesFoundInTheProject(Tileset tileset)
        {
            // Load all TilesetBrush assets found in the project
            string[] guids = AssetDatabase.FindAssets("t:TilesetBrush");
            foreach (string brushGuid in guids)
            {
                string brushAssetPath = AssetDatabase.GUIDToAssetPath(brushGuid);
                AssetDatabase.LoadAssetAtPath<TilesetBrush>(brushAssetPath);
            }
            // Get all loaded brushes
            TilesetBrush[] brushesFound = (TilesetBrush[])Resources.FindObjectsOfTypeAll(typeof(TilesetBrush));
            for (int i = 0; i < brushesFound.Length; ++i)
            {
                if (brushesFound[i].Tileset == tileset)
                {
                    tileset.AddBrush(brushesFound[i]);
                }
            }
            EditorUtility.SetDirty(tileset);
        }

        public static int DoGroupFieldLayout( Tileset tileset, string label, int groupIdx)
        {
            string groupName = tileset.BrushGroupNames[groupIdx];
            string[] groupList = tileset.BrushGroupNames.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            EditorGUI.BeginChangeCheck();
            int idx = EditorGUILayout.Popup(label, ArrayUtility.FindIndex(groupList, x => x == groupName), groupList);
            if( EditorGUI.EndChangeCheck() )
            {
                return ArrayUtility.FindIndex(tileset.BrushGroupNames, x => x == groupList[idx]);
            }
            return groupIdx;
        }

        public static ReorderableList CreateTileViewReorderableList(Tileset tileset)
        {
            ReorderableList tileViewRList = new ReorderableList( tileset.TileViews, typeof(TileView), true, true, true, true);
            tileViewRList.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) =>
            {
                GenericMenu menu = new GenericMenu();
                GenericMenu.MenuFunction addTileSelectionFunc = () =>
                {
                    TileSelection tileSelection = tileset.TileSelection.Clone();
                    tileSelection.FlipVertical(); // flip vertical to fit the tileset coordinate system ( from top to bottom )                   
                    tileset.AddTileView("new TileView", tileSelection);
                    EditorUtility.SetDirty(tileset);
                };
                GenericMenu.MenuFunction addBrushSelectionFunc = () =>
                {
                    TileSelection tileSelection = BrushBehaviour.CreateTileSelection();
                    tileset.AddTileView("new TileView", tileSelection);
                    EditorUtility.SetDirty(tileset);
                };
                GenericMenu.MenuFunction removeAllTileViewsFunc = () =>
                {
                    if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete all the TileViews?", "Yes", "No"))
                    {
                        tileset.RemoveAllTileViews();
                        EditorUtility.SetDirty(tileset);
                    }
                };
                if (tileset.TileSelection != null)
                    menu.AddItem(new GUIContent("Add Tile Selection to TileView"), false, addTileSelectionFunc);
                else
                    menu.AddDisabledItem(new GUIContent("Add Tile Selection to TileView"));
                
                if (BrushBehaviour.GetBrushTileset() == tileset && BrushBehaviour.CreateTileSelection() != null)
                    menu.AddItem(new GUIContent("Add Brush Selection to TileView"), false, addBrushSelectionFunc);
                                
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Remove All TileViews"), false, removeAllTileViewsFunc);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Sort By Name"), false, tileset.SortTileViewsByName);
                menu.ShowAsContext();
            };
            tileViewRList.onRemoveCallback = (ReorderableList list) =>
            {
                if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete the TileView?", "Yes", "No"))
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    EditorUtility.SetDirty(tileset);
                }
            };
            tileViewRList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "TileViews", EditorStyles.boldLabel);
                Texture2D btnTexture = tileViewRList.elementHeight == 0f ? EditorGUIUtility.FindTexture("winbtn_win_max_h") : EditorGUIUtility.FindTexture("winbtn_win_min_h");
                if (GUI.Button(new Rect(rect.width - rect.height, rect.y, rect.height, rect.height), btnTexture, EditorStyles.label))
                {
                    tileViewRList.elementHeight = tileViewRList.elementHeight == 0f ? EditorGUIUtility.singleLineHeight : 0f;
                    tileViewRList.draggable = tileViewRList.elementHeight > 0f;
                }
            };
            tileViewRList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (tileViewRList.elementHeight == 0f)
                    return;
                Rect rLabel = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                TileView tileView = tileViewRList.list[index] as TileView;
                if (index == tileViewRList.index)
                {
                    string newName = EditorGUI.TextField(rLabel, tileView.name);
                    if (newName != tileView.name)
                    {
                        tileset.RenameTileView(tileView.name, newName);
                    }
                }
                else
                {
                    EditorGUI.LabelField(rLabel, tileView.name);
                }
            };

            return tileViewRList;
        }

        public static ReorderableList CreateBrushReorderableList(Tileset tileset)
        {
            ReorderableList brushRList = new ReorderableList(tileset.Brushes, typeof(Tileset.BrushContainer), true, true, true, true);            
            brushRList.displayAdd = brushRList.displayRemove = false;
            brushRList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Brushes", EditorStyles.boldLabel);                
            };
            brushRList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                Tileset.BrushContainer brushContainer = tileset.Brushes[index];
                Rect rTile = rect; rTile.width = rTile.height = tileset.VisualTileSize.y;
                Rect tileUV = brushContainer.BrushAsset.GetAnimUV();
                if (tileUV != default(Rect))
                {
                    GUI.Box(new Rect(rTile.position - Vector2.one, rTile.size + 2 * Vector2.one), "");
                    GUI.DrawTextureWithTexCoords(rTile, tileset.AtlasTexture, tileUV, true);
                }

                Rect rTileId = rect;
                rTileId.x += rTile.width + 20; rTileId.width -= rTile.width + 20;
                rTileId.height = rect.height / 2;
                GUI.Label(rTileId, "Id(" + brushContainer.Id + ")");
            };

            return brushRList;
        }

        public static void DoGUIDrawTileFromTileData(Rect dstRect, uint tileData, Tileset tileset, Rect customUV = default(Rect))
        {
            int tileId = (int)(tileData & Tileset.k_TileDataMask_TileId);
            if (tileId != Tileset.k_TileId_Empty)
            {
                if ((tileData & Tileset.k_TileFlag_FlipH) != 0) GUIUtility.ScaleAroundPivot(new Vector2(1f, -1f), dstRect.center);
                if ((tileData & Tileset.k_TileFlag_FlipV) != 0) GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), dstRect.center);
                if ((tileData & Tileset.k_TileFlag_Rot90) != 0) GUIUtility.RotateAroundPivot(90f, dstRect.center);
                GUI.DrawTextureWithTexCoords(dstRect, tileset.AtlasTexture, customUV == default(Rect) ? tileset.Tiles[tileId].uv : customUV, true);
                GUI.matrix = Matrix4x4.identity;
            }
        }
    }

    internal class GroupMatrixGUI
    {
        public delegate bool GetValueFunc(int layerA, int layerB);
        public delegate void SetValueFunc(int layerA, int layerB, bool val);
        public static void DoGUI(string title, string[] groupNames, ref bool show, ref Vector2 scrollPos, GetValueFunc getValue, SetValueFunc setValue)
        {
            int num = 0;
            for (int i = 0; i < groupNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(groupNames[i]))
                {
                    num++;
                }
            }
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Space(0f);
            show = EditorGUILayout.Foldout(show, title);
            GUILayout.EndHorizontal();
            if (show)
            {
                scrollPos = GUILayout.BeginScrollView(scrollPos, new GUILayoutOption[]
				{
					GUILayout.MinHeight(120f),
					GUILayout.MaxHeight((float)(100 + (num + 1) * 16))
				});
                Rect rect = GUILayoutUtility.GetRect((float)(16 * num + 100), 100f);
                Rect topmostRect = GUIClip_topmostRect();//GUIClip.topmostRect;
                Vector2 vector = GUIClip_Unclip(new Vector2(rect.x, rect.y));//GUIClip.Unclip(new Vector2(rect.x, rect.y));
                int num2 = 0;
                for (int j = 0; j < groupNames.Length; j++)
                {
                    if (groupNames[j] != string.Empty)
                    {
                        float num3 = (float)(130 + (num - num2) * 16) - (topmostRect.width + scrollPos.x);
                        if (num3 < 0f)
                        {
                            num3 = 0f;
                        }
                        Vector3 pos = new Vector3((float)(130 + 16 * (num - num2)) + vector.y + vector.x + scrollPos.y - num3, vector.y + scrollPos.y, 0f);
                        GUI.matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, 90f), Vector3.one);
                        if (SystemInfo.graphicsDeviceVersion.StartsWith("Direct3D 9.0"))
                        {
                            GUI.matrix *= Matrix4x4.TRS(new Vector3(-0.5f, -0.5f, 0f), Quaternion.identity, Vector3.one);
                        }
                        GUI.Label(new Rect(2f - vector.x - scrollPos.y, scrollPos.y - num3, 100f, 16f), groupNames[j], "RightLabel");
                        num2++;
                    }
                }
                GUI.matrix = Matrix4x4.identity;
                num2 = 0;
                for (int k = 0; k < groupNames.Length; k++)
                {
                    if (groupNames[k] != string.Empty)
                    {
                        int num4 = 0;
                        Rect rect2 = GUILayoutUtility.GetRect((float)(30 + 16 * num + 100), 16f);
                        GUI.Label(new Rect(rect2.x + 30f, rect2.y, 100f, 16f), groupNames[k], "RightLabel");
                        for (int l = groupNames.Length - 1; l >= 0; l--)
                        {
                            if (groupNames[l] != string.Empty)
                            {
                                if (num4 < num - num2)
                                {
                                    GUIContent content = new GUIContent(string.Empty, groupNames[k] + "/" + groupNames[l]);
                                    bool flag = getValue(k, l);
                                    bool flag2 = GUI.Toggle(new Rect(130f + rect2.x + (float)(num4 * 16), rect2.y, 16f, 16f), flag, content);
                                    if (flag2 != flag)
                                    {
                                        setValue(k, l, flag2);
                                    }
                                }
                                num4++;
                            }
                        }
                        num2++;
                    }
                }
                GUILayout.EndScrollView();
            }
        }

        public static Vector2 GUIClip_Unclip(Vector2 pos)
        {
            if (s_GUIClip_Type == null) Initialize();
            return (Vector2)s_GUIClip_Unclip_Method.Invoke(null, new object[] { pos });
        }

        public static Rect GUIClip_topmostRect()
        {
            if (s_GUIClip_Type == null) Initialize();
            return (Rect)s_GUIClip_topmostRect_Property.GetValue(s_GUIClip_Type, null);
        }

        private static Type s_GUIClip_Type;
        private static PropertyInfo s_GUIClip_topmostRect_Property;
        private static MethodInfo s_GUIClip_Unclip_Method;
        private static void Initialize()
        {
            if (s_GUIClip_Type == null)
            {
                System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var A in assemblies)
                {
                    s_GUIClip_Type = A.GetType("UnityEngine.GUIClip");
                    if (s_GUIClip_Type != null)
                    {
                        break;
                    }
                }
                if (s_GUIClip_Type != null)
                {
                    s_GUIClip_topmostRect_Property = s_GUIClip_Type.GetProperty("topmostRect", BindingFlags.Static | BindingFlags.Public );
                    s_GUIClip_Unclip_Method = s_GUIClip_Type.GetMethod("Unclip", new Type[] { typeof(Vector2) });
                    Debug.Assert(s_GUIClip_topmostRect_Property != null, "s_GUIClip_topmostRect_Property Null");
                    Debug.Assert(s_GUIClip_Unclip_Method != null, "s_GUIClip_Unclip_Method Null");
                }
                else
                {
                    Debug.LogError("UnityEngine.GUIClip not found!");
                }
            }
        }
    }
}