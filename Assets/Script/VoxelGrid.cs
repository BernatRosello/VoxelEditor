using UnityEngine;

public enum VoxelType
{
    Empty,
    Cube,
    Smooth,
    Sharp
}

public class VoxelGrid : MonoBehaviour
{
    public int size = 16;
    private VoxelType[,,] grid;
    public System.Action OnGridChanged;

    void Awake()
    {
        grid = new VoxelType[size, size, size];
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                for (int z = 0; z < size; z++)
                {
                    grid[x, y, z] = VoxelType.Cube;
                }
    }

    public VoxelType Get(int x, int y, int z)
    {
        return grid[x, y, z];
    }

    public void Set(int x, int y, int z, VoxelType type)
    {
        Debug.Log($"Set voxel[{x},{y},{z}] from {grid[x, y, z]} to {type}");
        if (grid[x, y, z] != type)
        {
            grid[x, y, z] = type;
            OnGridChanged?.Invoke();
        }
    }

}

