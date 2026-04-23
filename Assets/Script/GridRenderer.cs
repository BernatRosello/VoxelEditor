using UnityEngine;
using System.Collections.Generic;

public class GridRenderer : MonoBehaviour
{
    public VoxelGrid grid;
    public GameObject cubePrefab;
    public Transform originOffset;

    [System.Serializable]
    public class RendererPrefabEntry
    {
        public VoxelMeshID meshID;
        public GameObject prefab;
    }

    [SerializeField]
    public List<RendererPrefabEntry> rendererPrefabs = new List<RendererPrefabEntry>();

    private List<GameObject> spawned = new List<GameObject>();
    public Bounds gridBounds;

    void Awake()
    {
        gridBounds = new Bounds(originOffset.position, Vector3.one * grid.size);
        Redraw();
        grid.OnGridChanged += Redraw;
    }

    void Redraw()
    {
        foreach (var go in spawned)
            Destroy(go);
        spawned.Clear();

        float half = (grid.size - 1) / 2f;
        originOffset.localPosition = new Vector3(-half, -half, -half);
        

        for (int x = 0; x < grid.size; x++)
            for (int y = 0; y < grid.size; y++)
                for (int z = 0; z < grid.size; z++)
                {
                    var voxelData = grid.Get(x, y, z);
                    if (voxelData.IsVoid()) continue;

                    GameObject prefab = GetPrefab(voxelData.meshId);
                    var voxelRender = Instantiate(prefab, originOffset);

                    voxelRender.transform.localPosition = new Vector3(x, y, z);
                    voxelRender.transform.localRotation = EditorState.ROTS[voxelData.orientationId];
                    if (voxelData.reflection > 0) voxelRender.transform.localScale = new Vector3(-1,1,1);
                    voxelRender.GetComponent<Renderer>().material.color = GetColor(voxelData.meshId);

                    spawned.Add(voxelRender);
                }
    }

    GameObject GetPrefab(VoxelMeshID id)
    {
        foreach (var entry in rendererPrefabs)
        {
            if (entry.meshID.Equals(id))
                return entry.prefab;
        }
        return null;
    }

    Color GetColor(VoxelMeshID voxId)
    {
        if (Voxel.IsVoid(voxId)) return Color.clear;
        if (Voxel.IsSmooth(voxId)) return new Color(1f, 0.5f, 0f);
        if (Voxel.IsSharp(voxId)) return Color.green;
        return Color.blue;
    }

    public bool ToVoxelCoordinates(Vector3 point, out Vector3Int voxelCoords)
    {
        Vector3 local = originOffset.InverseTransformPoint(point) + Vector3.one * 0.5f;

        int x = Mathf.FloorToInt(local.x);
        int y = Mathf.FloorToInt(local.y);
        int z = Mathf.FloorToInt(local.z);
        voxelCoords = new Vector3Int(x, y, z);

        if ((x < 0) || (x >= grid.size) ||
            (y < 0) || (y >= grid.size) ||
            (z < 0) || (z >= grid.size))
        {
            return false;
        }
        return true;
    }
}
