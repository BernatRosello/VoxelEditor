#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[CustomPropertyDrawer(typeof(NavMeshAreaAttribute))]
public class NavMeshAreaDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        bool valid = IsValidArea(property.stringValue);

        return valid
            ? EditorGUIUtility.singleLineHeight
            : EditorGUIUtility.singleLineHeight * 2.5f;
    }

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.HelpBox(
                position,
                "[NavMeshArea] can only be used on string fields.",
                MessageType.Error);

            return;
        }

        string[] areaNames = NavMesh.GetAreaNames();

        Rect popupRect = position;
        popupRect.height = EditorGUIUtility.singleLineHeight;

        int selectedIndex = System.Array.IndexOf(
            areaNames,
            property.stringValue);

        bool valid = selectedIndex >= 0;

        if (areaNames.Length > 0)
        {
            selectedIndex = Mathf.Max(0, selectedIndex);

            selectedIndex = EditorGUI.Popup(
                popupRect,
                label.text,
                selectedIndex,
                areaNames);

            property.stringValue = areaNames[selectedIndex];
        }

        if (!valid && !string.IsNullOrEmpty(property.stringValue))
        {
            Rect helpRect = position;
            helpRect.y += EditorGUIUtility.singleLineHeight + 2f;
            helpRect.height = EditorGUIUtility.singleLineHeight * 1.5f;

            EditorGUI.HelpBox(
                helpRect,
                $"NavMesh Area '{property.stringValue}' no longer exists.",
                MessageType.Warning);
        }
    }

    private static bool IsValidArea(string areaName)
    {
        return !string.IsNullOrEmpty(areaName)
            && NavMesh.GetAreaFromName(areaName) != -1;
    }
}

#endif