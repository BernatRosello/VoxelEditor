#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NavigationAnimator))]
public class NavigationAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject,
            "m_Script",
            "navSurfTransform",
            "surfaceMask",
            "surfaceRayDistance");

        var mode = serializedObject.FindProperty("navSurfMode");
        EditorGUILayout.PropertyField(mode);

        switch ((NavSurfaceMode)mode.enumValueIndex)
        {
            case NavSurfaceMode.FlatTransform:
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("navSurfTransform"),
                    new GUIContent("Surface Transform"));
                break;

            case NavSurfaceMode.SphereTransform:
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("navSurfTransform"),
                    new GUIContent("Sphere Center"));
                break;

            case NavSurfaceMode.RaycastSurface:
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("surfaceMask"));

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("surfaceRayDistance"));
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif