#if UNITY_EDITOR
using UnityEngine;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine.AI;
using Unity.AI.Navigation.Editor;
using System.Collections.Generic;

[ExecuteAlways]
public class NavMeshNoPaddingBaker : MonoBehaviour
{
    [Tooltip("Agent type used ONLY for baking (should have near-zero radius)")]
    public int bakingAgentTypeID;

    public void Bake()
    {
        var surface = GetComponent<NavMeshSurface>();

        if (surface == null)
        {
            Debug.LogError("NavMeshSurface not found.");
            return;
        }

        // Store original agent
        int originalAgentTypeID = surface.agentTypeID;

        // Swap to baking agent
        surface.agentTypeID = bakingAgentTypeID;

        // Use Unity's built-in bake
        surface.BuildNavMesh();

        // Restore original
        surface.agentTypeID = originalAgentTypeID;

        Debug.Log("NavMesh baked using custom baking agent (no padding).");
    }
}


[CustomEditor(typeof(NavMeshNoPaddingBaker))]
public class NavMeshNoPaddingBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NavMeshNoPaddingBaker baker = (NavMeshNoPaddingBaker)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Bake NavMesh (No Padding)"))
        {
            baker.Bake();
        }
    }
}
#endif