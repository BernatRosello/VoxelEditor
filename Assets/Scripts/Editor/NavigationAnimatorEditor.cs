#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NavigationAnimator))]
public class NavigationAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script", "navSettings");

        NavigationAnimator animator =
            (NavigationAnimator)target;

        NavigationAnimatorSettings settings =
            animator.navSettings;

        EditorGUILayout.Space();

        if (settings == null)
        {
            EditorGUILayout.HelpBox(
                "No Navigation Animator Settings asset assigned.",
                MessageType.Error);

            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.ObjectField(
            "Settings Asset",
            settings,
            typeof(NavigationAnimatorSettings),
            false);

        if (GUILayout.Button("Open Settings"))
        {
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        DrawSettingsWarnings(settings);

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawSettingsWarnings(
        NavigationAnimatorSettings settings)
    {
        switch (settings.NavSurfMode)
        {
            case NavSurfaceMode.FlatTransform:
            case NavSurfaceMode.SphereTransform:
                break;

            case NavSurfaceMode.RaycastSurface:

                if (settings.SurfaceRayDistance <= 0f)
                {
                    EditorGUILayout.HelpBox(
                        "Surface Ray Distance should be greater than zero.",
                        MessageType.Warning);
                }

                break;
        }
    }
}

#endif