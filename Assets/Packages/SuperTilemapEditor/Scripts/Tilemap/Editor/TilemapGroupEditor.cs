using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;

namespace CreativeSpore.SuperTilemapEditor
{
    [CustomEditor(typeof(TilemapGroup))]
    public class TilemapGroupEditor : Editor
    {
        [MenuItem("GameObject/SuperTilemapEditor/TilemapGroup", false, 10)]
        static void CreateTilemap()
        {
            GameObject obj = new GameObject("TilemapGroup");
            obj.AddComponent<TilemapGroup>();
        }

        private ReorderableList m_tilemapReordList;
        private TilemapEditor m_tilemapEditor;

        private void OnEnable()
        {
            var targetObj = target as TilemapGroup;
            targetObj.Refresh();
            m_tilemapReordList = CreateTilemapReorderableList();
            m_tilemapReordList.index = serializedObject.FindProperty("m_selectedIndex").intValue;
        }

        public override void OnInspectorGUI()
        {
            var targetObj = target as TilemapGroup;
            // NOTE: this happens during undo/redo
            if( targetObj.transform.childCount != targetObj.Tilemaps.Count) 
            {
                targetObj.Refresh();
            }
            serializedObject.Update();

            // clamp index to valid value
            serializedObject.FindProperty("m_selectedIndex").intValue = m_tilemapReordList.index = Mathf.Clamp(m_tilemapReordList.index, -1, targetObj.Tilemaps.Count - 1);

            // Draw Tilemap List
            m_reordIdx = 0;
            m_tilemapReordList.DoLayoutList();
            EditorGUILayout.Space();

            // Draw Tilemap Inspector
            TilemapEditor tilemapEditor = GetTilemapEditor();
            if (tilemapEditor)
            {
                tilemapEditor.OnInspectorGUI();
            }

            serializedObject.ApplyModifiedProperties();            

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
                Repaint();
            }
        }

        public void OnSceneGUI()
        {
            TilemapEditor tilemapEditor = GetTilemapEditor();
            if (tilemapEditor)
            {
                (tilemapEditor as TilemapEditor).OnSceneGUI();                
            }
        }

        //NOTE: m_tilemapEditor.target changes when OnSceneGUI is called, so this method makes sure to create it again if target doesn't match
        private TilemapEditor GetTilemapEditor()
        {
            var targetObj = target as TilemapGroup;
            if (!m_tilemapEditor || !m_tilemapEditor.target || m_tilemapEditor.target != targetObj.SelectedTilemap)
            {
                if (targetObj.SelectedTilemap)
                {
                    m_tilemapEditor = TilemapEditor.CreateEditor(targetObj.SelectedTilemap) as TilemapEditor;
                }
                else
                {
                    m_tilemapEditor = null;
                }
            }
            return m_tilemapEditor;
        }

        private Dictionary<int, Rect> m_reordListRectsDic = new Dictionary<int, Rect>();
        private int m_reordIdx;
        private ReorderableList CreateTilemapReorderableList()
        {
            ReorderableList reordList = new ReorderableList(serializedObject, serializedObject.FindProperty("m_tilemaps"), true, true, true, true);
            reordList.displayAdd = reordList.displayRemove = true;
            reordList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Tilemaps", EditorStyles.boldLabel);
                Texture2D btnTexture = reordList.elementHeight == 0f ? EditorGUIUtility.FindTexture("winbtn_win_max_h") : EditorGUIUtility.FindTexture("winbtn_win_min_h");
                if (GUI.Button(new Rect(rect.width - rect.height, rect.y, rect.height, rect.height), btnTexture, EditorStyles.label))
                {
                    reordList.elementHeight = reordList.elementHeight == 0f ? EditorGUIUtility.singleLineHeight : 0f;
                    reordList.draggable = reordList.elementHeight > 0f;
                }
            };
            reordList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = reordList.serializedProperty.GetArrayElementAtIndex(index);
                
                if (Event.current.type == EventType.Repaint)
                {
                    m_reordListRectsDic[m_reordIdx] = rect;
                }
                else if (Event.current.type == EventType.Layout)
                {
                    m_reordListRectsDic.TryGetValue(m_reordIdx, out rect);
                }
                m_reordIdx++;

                rect.y += 2;

                Tilemap tilemap = element.objectReferenceValue as Tilemap;
                SerializedObject tilemapSerialized = new SerializedObject(tilemap);
                SerializedObject tilemapObjSerialized = new SerializedObject(tilemapSerialized.FindProperty("m_GameObject").objectReferenceValue);
                
                GUILayout.BeginArea(rect);
                EditorGUILayout.BeginHorizontal();
                tilemap.IsVisible = EditorGUILayout.Toggle(tilemap.IsVisible, GUILayout.Width(16));
                EditorGUILayout.PropertyField(tilemapObjSerialized.FindProperty("m_Name"), GUIContent.none);
                if (TilemapEditor.EditMode == TilemapEditor.eEditMode.Collider)
                {
                    SerializedProperty colliderTypeProperty = tilemapSerialized.FindProperty("ColliderType");
                    string[] colliderTypeNames = new List<string>(System.Enum.GetNames(typeof(eColliderType)).Select(x => x.Replace('_', ' '))).ToArray();
                    EditorGUI.BeginChangeCheck();
                    colliderTypeProperty.intValue = GUILayout.SelectionGrid(colliderTypeProperty.intValue, colliderTypeNames, colliderTypeNames.Length, GUILayout.MaxHeight(0.9f * EditorGUIUtility.singleLineHeight));
                    if (EditorGUI.EndChangeCheck())
                    {
                        tilemapSerialized.ApplyModifiedProperties();
                        tilemap.Refresh(false, true);
                    }
                }
                else
                {
                    // Sorting Layer and Order in layer            
                    EditorGUI.BeginChangeCheck();
                    EditorGUIUtility.labelWidth = 1;
                    EditorGUILayout.PropertyField(tilemapSerialized.FindProperty("m_sortingLayer"), new GUIContent(" "));
                    EditorGUIUtility.labelWidth = 40;
                    EditorGUILayout.PropertyField(tilemapSerialized.FindProperty("m_orderInLayer"), new GUIContent("Order"), GUILayout.MaxWidth(90));
                    EditorGUIUtility.labelWidth = 0;
                    tilemapSerialized.FindProperty("m_orderInLayer").intValue = (tilemapSerialized.FindProperty("m_orderInLayer").intValue << 16) >> 16; // convert from int32 to int16 keeping sign
                    if (EditorGUI.EndChangeCheck())
                    {
                        tilemapSerialized.ApplyModifiedProperties();
                        tilemap.RefreshChunksSortingAttributes();
                        SceneView.RepaintAll();
                    }
                    //--- 
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();

                if(GUI.changed)
                {
                    tilemapObjSerialized.ApplyModifiedProperties();
                }
            };
            reordList.onReorderCallback = (ReorderableList list) =>
            {
                var targetObj = target as TilemapGroup;
                int sibilingIdx = 0;
                foreach (Tilemap tilemap in targetObj.Tilemaps)
                {
                    tilemap.transform.SetSiblingIndex(sibilingIdx++);
                }
                Repaint();
            };
            reordList.onSelectCallback = (ReorderableList list) =>
            {
                serializedObject.FindProperty("m_selectedIndex").intValue = reordList.index;
                serializedObject.ApplyModifiedProperties();
                GUI.changed = true;
                TileSelectionWindow.RefreshIfVisible();
                TilePropertiesWindow.RefreshIfVisible();
            };
            reordList.onAddCallback = (ReorderableList list) =>
            {
                var targetObj = target as TilemapGroup;
                Undo.RegisterCompleteObjectUndo(targetObj, "New Tilemap");
                GameObject obj = new GameObject();
                Undo.RegisterCreatedObjectUndo(obj, "New Tilemap");
                Tilemap newTilemap = obj.AddComponent<Tilemap>();
                obj.transform.parent = targetObj.transform;
                obj.name = GameObjectUtility.GetUniqueNameForSibling(obj.transform.parent, "New Tilemap");

                Tilemap copiedTilemap = targetObj.SelectedTilemap;
                if(copiedTilemap)
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(copiedTilemap);
                    UnityEditorInternal.ComponentUtility.PasteComponentValues(newTilemap);
                    obj.SendMessage("_DoDuplicate");
                    obj.name = GameObjectUtility.GetUniqueNameForSibling(obj.transform.parent, copiedTilemap.name);
                }
            };
            reordList.onRemoveCallback = (ReorderableList list) =>
            {
                var targetObj = target as TilemapGroup;
                Undo.DestroyObjectImmediate(targetObj.SelectedTilemap.gameObject);
                //NOTE: Fix argument exception
                if (m_tilemapReordList.index == targetObj.Tilemaps.Count - 1)
                {
                    serializedObject.FindProperty("m_selectedIndex").intValue = m_tilemapReordList.index = m_tilemapReordList.index - 1;
                }
            };

            return reordList;
        }
    }
}