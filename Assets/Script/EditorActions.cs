using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class EditorActions : MonoBehaviour
{
    public Transform gridOriginTransform;
    public static EditorActions Instance;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(this);
    }
    public static void SetOrientation(float rotationSlider)
    {
        EditorState.Instance.activeOrientation = (int)rotationSlider;
    }

    public static void SetReflection(int reflect)
    {
        EditorState.Instance.activeReflection = reflect;
    }

    public static void SetActiveVoxelMesh(int voxelID)
    {
        EditorState.Instance.activeMesh = (VoxelMeshID)voxelID;
    }

}
