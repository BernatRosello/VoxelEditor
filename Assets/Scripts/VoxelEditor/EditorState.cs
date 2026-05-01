using System;
using UnityEngine;


public class EditorState : MonoBehaviour
{
    public static EditorState Instance;


    public int activeOrientation = 0;
    public int activeReflection = 0;
    public VoxelMeshID activeMesh;
    public System.Action OnStateChanged;
    public Vector3 debugReflectionVector;

    void Awake()
    {
        Instance = this;
    }
}
