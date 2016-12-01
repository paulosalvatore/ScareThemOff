using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Reflection;

namespace CreativeSpore.SuperTilemapEditor 
{
	[CustomPropertyDrawer(typeof(SortingLayerAttribute))]
	public class SortingLayerPropertyDrawer : PropertyDrawer 
    {
        private bool m_hasChanged = false;
        private SerializedProperty m_property;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) 
        {
            m_property = property;
			if (property.propertyType != SerializedPropertyType.Integer) 
            {
                Debug.LogError("SortedLayer property should be an integer ( the layer id )");
			}
            else
            {
                if (m_hasChanged)
                {
                    m_hasChanged = false;
                    GUI.changed = true;
                }

                SortingLayerField(position, label, property, EditorStyles.popup, EditorStyles.label);            
            }
		}

        // Get the sorting layer names
        public static string[] GetSortingLayerNames()
        {
            System.Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
            return (string[])sortingLayersProperty.GetValue(null, new object[0]);
        }

        // Get the sorting layer UniqueIds
        public static int[] GetSortingLayerUniqueIDs()
        {
            System.Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
            return (int[])sortingLayersProperty.GetValue(null, new object[0]);
        }

        public void SortingLayerFieldLayout(GUIContent label, SerializedProperty layerID, GUIStyle style, GUIStyle labelStyle)
        {
            Rect position = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.popup, new GUILayoutOption[0]);
            SortingLayerField(position, label, layerID, style, labelStyle);
        }

        public void SortingLayerField(Rect position, GUIContent label, SerializedProperty layerID, GUIStyle style, GUIStyle labelStyle)
        {
            Event e = Event.current;
            int[] sortingLayerUniqueIDs = EditorUtils.GetSortingLayerUniqueIDs();
            string[] sortingLayerNames = EditorUtils.GetSortingLayerNames();

            ArrayUtility.Add<string>(ref sortingLayerNames, string.Empty);
            ArrayUtility.Add<string>(ref sortingLayerNames, "Add Sorting Layer...");

            GUIContent[] array = new GUIContent[sortingLayerNames.Length];
            for (int i = 0; i < sortingLayerNames.Length; i++)
            {
                array[i] = new GUIContent(sortingLayerNames[i]);
            }

            EditorUtility.SelectMenuItemFunction setEnumValueDelegate = (object userData, string[] options, int selected) =>
            {
                if (selected == options.Length - 1)
                {
                    //((TagManager)EditorApplication.tagManager).m_DefaultExpandedFoldout = "SortingLayers";
                    PropertyInfo tagManagerPropertyInfo = typeof(EditorApplication).GetProperty("tagManager", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetProperty);
                    if (tagManagerPropertyInfo != null)
                    {
                        System.Object tagManager = (System.Object)tagManagerPropertyInfo.GetValue(typeof(EditorApplication), null);
                        FieldInfo fieldInfo = tagManager.GetType().GetField("m_DefaultExpandedFoldout");
                        if (fieldInfo != null)
                        {
                            fieldInfo.SetValue(tagManager, "SortingLayers");
                        }
                    }
                    EditorApplication.ExecuteMenuItem("Edit/Project Settings/Tags and Layers");
                }
                else
                {
                    m_hasChanged = true;
                    layerID.intValue = m_property.intValue = sortingLayerUniqueIDs[selected];
                    m_property.serializedObject.ApplyModifiedProperties();
                    m_property.serializedObject.Update();
                    EditorUtility.SetDirty(m_property.serializedObject.targetObject);
                }
            };

            int sortingLayerIdx = System.Array.IndexOf(sortingLayerUniqueIDs, layerID.intValue);

            Rect rPopup = position;
            rPopup.x += EditorGUIUtility.labelWidth;
            rPopup.width -= EditorGUIUtility.labelWidth;

            if (e.type == EventType.Repaint)
            {
                labelStyle.Draw(position, label, false, false, false, false);
                string sortingLayerName = sortingLayerIdx >= 0 && sortingLayerIdx < sortingLayerNames.Length ? sortingLayerNames[sortingLayerIdx] : "";
                style.Draw(rPopup, sortingLayerName, false, false, false, false);
            }

            if (position.Contains(e.mousePosition) && e.isMouse && e.button == 0)
            {
                EditorUtility.DisplayCustomMenu(rPopup, array, sortingLayerIdx, setEnumValueDelegate, null);
            }
        }
	}
}