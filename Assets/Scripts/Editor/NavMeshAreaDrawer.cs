#if UNITY_EDITOR

using System;
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
        bool invalid =
            !string.IsNullOrEmpty(property.stringValue) &&
            !IsValidArea(property.stringValue);

        return invalid
            ? EditorGUIUtility.singleLineHeight * 2.5f
            : EditorGUIUtility.singleLineHeight;
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

        if (areaNames.Length == 0)
        {
            EditorGUI.LabelField(
                position,
                label.text,
                "No NavMesh Areas defined");

            return;
        }

        bool invalid =
            !string.IsNullOrEmpty(property.stringValue) &&
            !IsValidArea(property.stringValue);

        string[] popupOptions;

        int selectedIndex;

        if (invalid)
        {
            popupOptions = new string[areaNames.Length + 1];
            popupOptions[0] = $"<Missing> {property.stringValue}";

            Array.Copy(
                areaNames,
                0,
                popupOptions,
                1,
                areaNames.Length);

            selectedIndex = 0;
        }
        else
        {
            popupOptions = areaNames;

            selectedIndex = Array.IndexOf(
                areaNames,
                property.stringValue);

            if (selectedIndex < 0)
                selectedIndex = 0;
        }

        Rect popupRect = position;
        popupRect.height = EditorGUIUtility.singleLineHeight;

        int newIndex = EditorGUI.Popup(
            popupRect,
            label.text,
            selectedIndex,
            popupOptions);

        if (invalid)
        {
            if (newIndex > 0)
                property.stringValue = popupOptions[newIndex];
        }
        else
        {
            property.stringValue = popupOptions[newIndex];
        }

        if (invalid)
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
        return !string.IsNullOrEmpty(areaName) &&
               NavMesh.GetAreaFromName(areaName) != -1;
    }
}

#endif