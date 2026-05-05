using UnityEngine;

// Optional editor button
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(NavMeshAutoLinker))]
public class NavMeshAutoLinkerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NavMeshAutoLinker linker = (NavMeshAutoLinker)target;

        if (GUILayout.Button("Rebuild NavMesh Links"))
        {
            linker.RebuildLinks();
        }
    }
}
#endif
