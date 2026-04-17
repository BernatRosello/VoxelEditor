using UnityEngine;


public enum VoxelTool
{
    Delete,
    Cube,
    Smooth,
    Sharp
}

public class EditorState : MonoBehaviour
{
    public static EditorState Instance;

    public VoxelTool activeTool = VoxelTool.Delete;

    public int sliceX = 0;
    public int sliceY = 0;
    public int sliceZ = 0;

    void Awake()
    {
        sliceX = -1;
        sliceY = -1;
        sliceZ = -1;
        Instance = this;
    }
}
