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
    public Vector3 prefabScale = new Vector3(1, 1, 1);

    private List<GameObject> spawned = new List<GameObject>();
    public Bounds gridBounds;
    public Transform previewVoxelParent;
    GameObject previewInstance;

    void Awake()
    {
        gridBounds = new Bounds(originOffset.position, Vector3.one * grid.size);
        Redraw();
        grid.OnGridChanged += Redraw;
        EditorState.Instance.OnStateChanged += RedrawPreviewVoxel;
    }

    void RedrawPreviewVoxel()
    {
        // I need to draw a preview voxel that will visualize the active voxel properties of the EditorState.Instance, this redraw function should be called whenever the active options change (you may add a callback to do this).
        // The preview voxel should be represented as if it were a GUI element or something of the sort, but should match the viewing angle of the grid so the orientation can be interpreted correctly despite of the view angle of the camera relative to the voxel grid.
        var state = EditorState.Instance;

        if (Voxel.IsVoid(state.activeMesh))
        {
            if (previewInstance != null)
                previewInstance.SetActive(false);
            return;
        }

        GameObject prefab = GetPrefab(state.activeMesh);
        if (prefab == null) return;

        // Create if needed
        if (previewInstance == null)
        {
            previewInstance = Instantiate(prefab, previewVoxelParent);
        }
        else
        {
            // If mesh type changed → rebuild
            if (previewInstance.name.Contains(prefab.name) == false)
            {
                Destroy(previewInstance);
                previewInstance = Instantiate(prefab, previewVoxelParent);
            }
        }

        previewInstance.SetActive(true);
        previewInstance.layer = previewVoxelParent.gameObject.layer;

        // 🔹 Reset transform position
        previewInstance.transform.localPosition = Vector3.zero;
        // Rotation
        previewInstance.transform.localRotation = Voxel.GetOrientation(state.activeOrientation);
        // Reflection & scale
        previewInstance.transform.localScale = Vector3.Scale(Voxel.GetReflectVector(state.activeReflection), prefabScale);


        // Color
        var renderer = previewInstance.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = GetColor(state.activeMesh);
        }

        // 🔹 Position it like UI (in front of camera)
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 forward = cam.transform.forward;

            // Fixed distance in front of camera
            Vector3 worldPos = cam.transform.position + forward * 2.0f;

            previewVoxelParent.position = worldPos;

            // 🔥 KEY PART:
            // Align with world/grid, NOT camera
            previewVoxelParent.rotation = Quaternion.identity;
        }
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
                    voxelRender.transform.localRotation = voxelData.GetOrientation();
                    voxelRender.transform.localScale = Vector3.Scale(voxelData.GetScale(), prefabScale);
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
        Debug.LogError($"Couldn't get prefab for VoxelMeshID {id} (unregistered value {(int)id})");
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
