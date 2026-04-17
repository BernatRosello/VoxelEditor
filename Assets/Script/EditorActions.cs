using UnityEngine;

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
    public static void SetTool(int toolIndex)
    {
        EditorState.Instance.activeTool = (VoxelTool)toolIndex;
    }

    public static void SetSliceZ(float value)
    {
        EditorState.Instance.sliceZ = Mathf.RoundToInt(value);
    }

}
