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
        Debug.Log($"Set activeOrientation: {(int)rotationSlider}, corresponding to ROTS[{(int)rotationSlider}]={Voxel.GetOrientation((int)rotationSlider)}");
        EditorState.Instance.activeOrientation = (int)rotationSlider;
    EditorState.Instance.OnStateChanged?.Invoke();
    }

    public static void SetReflection(bool reflect)
    {
        Debug.Log($"Set activeReflection: {reflect}");
        EditorState.Instance.activeReflection = reflect ? 1 : 0;
    EditorState.Instance.OnStateChanged?.Invoke();
    }

    public static void SetActiveVoxelMesh(int voxelID)
    {
        Debug.Log($"Set activeMesh: {(VoxelMeshID)voxelID}");
        EditorState.Instance.activeMesh = (VoxelMeshID)voxelID;
    EditorState.Instance.OnStateChanged?.Invoke();
    }

}
