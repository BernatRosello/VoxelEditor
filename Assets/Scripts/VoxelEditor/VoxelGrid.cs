using System.Collections.Generic;
using System.Drawing;
using UnityEditor;
using UnityEngine;

public struct Voxel
{

    public VoxelMeshID meshId;
    public int orientationId;
    public int reflection;

    public override string ToString()
    {
        return $"MeshID: {meshId} Orientation: {orientationId} Reflection: {reflection}";
    }
    public bool IsSharp() => IsSharp(meshId);
    public bool IsCurve() => IsSharp(meshId);
    public bool IsVoid() => IsVoid(meshId);
    public Vector3 GetScale() => GetReflectVector(reflection);
    public Quaternion GetOrientation() => GetOrientation(orientationId);
    public static bool IsSharp(Voxel v) => IsSharp(v.meshId);
    public static bool IsSmooth(Voxel v) => IsSharp(v.meshId);
    public static bool IsVoid(Voxel v) => IsVoid(v.meshId);
    public static  bool IsSharp(VoxelMeshID id)
    {
        return id switch
        {
            VoxelMeshID.VVVVVV => false,
            VoxelMeshID.FFFFFF => false,
            VoxelMeshID.CCCFFF => false,
            VoxelMeshID.SSSFFF => true,
            VoxelMeshID.VCCFFF => false,
            VoxelMeshID.VCVCFF => false,
            VoxelMeshID.VSSFFF => true,
            VoxelMeshID.VSVSFF => true,
            VoxelMeshID.VVVCCC => false,
            VoxelMeshID.VVVCCF => false,
            VoxelMeshID.VVVSSC => true,
            VoxelMeshID.VVVSSF => true,
            VoxelMeshID.VVVSSS => true,
            _ => false
        };
    }

    public static bool IsSmooth(VoxelMeshID id)
    {
        return id switch
        {
            VoxelMeshID.VVVVVV => false,
            VoxelMeshID.FFFFFF => false,
            VoxelMeshID.CCCFFF => true,
            VoxelMeshID.SSSFFF => false,
            VoxelMeshID.VCCFFF => true,
            VoxelMeshID.VCVCFF => true,
            VoxelMeshID.VSSFFF => false,
            VoxelMeshID.VSVSFF => false,
            VoxelMeshID.VVVCCC => true,
            VoxelMeshID.VVVCCF => true,
            VoxelMeshID.VVVSSC => false,
            VoxelMeshID.VVVSSF => false,
            VoxelMeshID.VVVSSS => false,
            _ => false
        };
    }
    public static bool IsVoid(VoxelMeshID id) { return id == VoxelMeshID.VVVVVV; }
    
    public static readonly Quaternion[] ROTS = new Quaternion[8]
    {
        // 0 (+,+,-)
        Quaternion.Euler(180, 0, -90),
        // 1 (+,+,+) ← canonical
        Quaternion.identity,
        // 2 (-,+,+)
        Quaternion.Euler(0, 0, 90),
        // 3 (-,+,-)
        Quaternion.Euler(180, 0, 180),
        // 4 (+,-,-)
        Quaternion.Euler(180, 0, 0),
        // 5 (+,-,+)
        Quaternion.Euler(0, 0, -90),
        // 6 (-,-,+)
        Quaternion.Euler(0, 0, 180),
        // 7 (-,-,-)
        Quaternion.Euler(180, 0, 90),
    };
    public static Quaternion GetOrientation(int orientation)
    {
        return ROTS[orientation];
    }

    public static Vector3 GetReflectVector(int reflect)
    {
        if (reflect > 0)
            //return new Vector3(-1,-1,-1);
            return EditorState.Instance.debugReflectionVector;
        else
            return Vector3.one;
    }
}

public enum VoxelMeshID
{
    VVVVVV = 0,
    FFFFFF,
    CCCFFF,
    SSSFFF,
    VCCFFF,
    VCVCFF,
    VSSFFF,
    VSVSFF,
    VVVCCC,
    VVVCCF,
    VVVSSC,
    VVVSSF,
    VVVSSS,
}



[CreateAssetMenu(fileName = "VoxelMeshes", menuName = "VoxelMeshes")]
public class VoxelMeshes : ScriptableSingleton<VoxelMeshes>
{
    Dictionary<VoxelMeshID, Mesh> _meshDict;

    public Mesh this[VoxelMeshID id]
    {
        get { return _meshDict[id]; }
    }
    public Mesh this[int id]
    {
        get
        {
            if (!_meshDict.ContainsKey((VoxelMeshID)id))
                return null;
            return _meshDict[(VoxelMeshID)id];
        }
    }
}

[CreateAssetMenu(fileName = "VoxelGridData", menuName = "VoxelGridData")]
public class VoxelGridData : ScriptableObject
{

    private int _size;
    private Voxel[,,] _grid;

    public VoxelGridData(int size = 0)
    {
        _size = size;
        _grid = new Voxel[size, size, size];
    }
    public Voxel this[int x, int y, int z]
    {
        get { return _grid[x, y, z]; }
        set { _grid[x, y, z] = value; }
    }
    public int size { get { return _size; }}

    public void ClearResize(int size)
    {
        _size = size;
        _grid = new Voxel[size,size,size];
        Clear();
    }
    public void Fill(VoxelMeshID fillvoxel) { 
        Voxel fill = new();
        fill.meshId = fillvoxel;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                for (int z = 0; z < size; z++)
                {
                    _grid[x, y, z] = fill;
                }}
    public void Clear()
    {
        Voxel init = new();
        init.meshId = VoxelMeshID.VVVVVV;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                for (int z = 0; z < size; z++)
                {
                    _grid[x, y, z] = init;
                }
    }
}

public class VoxelGrid : MonoBehaviour
{
    public int configSize = 16;
    private VoxelGridData gridData;
    public System.Action OnGridChanged;

    public int size { get { return gridData.size; }}

    void Awake()
    {
        gridData = ScriptableObject.CreateInstance<VoxelGridData>();
        gridData.ClearResize(configSize);
        gridData.Fill(VoxelMeshID.FFFFFF);
    }

    public Voxel Get(int x, int y, int z)
    {
        return gridData[x, y, z];
    }

    public void Set(int x, int y, int z, Voxel voxel)
    {
        Debug.Log($"Set voxel[{x},{y},{z}] from {gridData[x, y, z]} to {voxel}");
        gridData[x, y, z] = voxel;
        OnGridChanged?.Invoke();
    }

}

