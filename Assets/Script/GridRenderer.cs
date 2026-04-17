using UnityEngine;
using System.Collections.Generic;

public class GridRenderer : MonoBehaviour
{
    public VoxelGrid grid;
    public GameObject cubePrefab;
    public Transform originOffset;

    private List<GameObject> spawned = new List<GameObject>();
    public Bounds gridBounds;

    void Awake()
    {
        gridBounds = new Bounds(originOffset.position, Vector3.one * grid.size);
        Redraw();
        grid.OnGridChanged += Redraw;
    }

    void Update()
    {
        
    }

    void Redraw()
    {
        foreach (var go in spawned)
            Destroy(go);
        spawned.Clear();

        int sliceZ = EditorState.Instance.sliceZ;

        float half = (grid.size - 1) / 2f;
        originOffset.localPosition = new Vector3(-half, -half, -half);

        for (int x = 0; x < grid.size; x++)
            for (int y = 0; y < grid.size; y++)
                for (int z = 0; z < grid.size; z++)
                {
                    var type = grid.Get(x, y, z);
                    if (type == VoxelType.Empty) continue;

                    var voxel = Instantiate(cubePrefab, originOffset);
                    voxel.transform.localPosition = new Vector3(x, y, z);

                    var newColor = GetColor(type);
                    if (sliceZ >= 0 && sliceZ < grid.size && z != sliceZ)
                        newColor.a = (1 - Mathf.Abs(z - sliceZ) / grid.size) * 0.1f;
                    voxel.GetComponent<Renderer>().material.color = newColor;

                    spawned.Add(voxel);
                }
    }

    Color GetColor(VoxelType type)
    {
        return type switch
        {
            VoxelType.Cube => Color.blue,
            VoxelType.Smooth => new Color(1f, 0.5f, 0f),
            VoxelType.Sharp => Color.green,
            _ => Color.clear
        };
    }

    public bool ToVoxelCoordinates(Vector3 point, out Vector3Int voxelCoords)
    {
        Vector3 local = originOffset.InverseTransformPoint(point) + Vector3.one * 0.5f;

        int x = Mathf.FloorToInt(local.x);
        int y = Mathf.FloorToInt(local.y);
        int z = Mathf.FloorToInt(local.z);
        voxelCoords = new Vector3Int(x, y, z);
        // Debug.Log($"Pre Flooring coords {local}, post flooring: ({x},{y},{z})");
        if ((x < 0) || (x >= grid.size) ||
            (y < 0) || (y >= grid.size) ||
            (z < 0) || (z >= grid.size))
        {
            // Debug.LogWarning("Point out of voxel grid (coordinates OUT OF GRID RANGE)");
            return false;
        }
        return true;
    }

}

