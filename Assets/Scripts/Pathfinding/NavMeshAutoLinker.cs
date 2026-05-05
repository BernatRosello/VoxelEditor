using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[ExecuteAlways]
public class NavMeshAutoLinker : MonoBehaviour
{
    public int resolution = 16;
    public float linkWidth = 0.1f;
    [Range(0.0f, 3.0f)]
    public float temp = 0.1f;
    [Range(0.0f, 1.0f)]
    public float quadWidthPercent = 0.9f;
    [Header("Width Compensation")]
    public AnimationCurve widthRamp = AnimationCurve.Linear(0, 1, 1, 1);

    public float gizmoSize = 1f;

    private Vector3[][][] edges; // [face][edge][i]

    enum Face { PX, NX, PY, NY, PZ, NZ }

    public void RebuildLinks()
    {
        GenerateEdges();
        BuildLinks();
    }

    // =========================================
    // EDGE GENERATION (STRUCTURED)
    // =========================================
    void GenerateEdges()
    {
        edges = new Vector3[6][][];

        for (int f = 0; f < 6; f++)
        {
            edges[f] = GenerateFaceEdges((Face)f);
        }
    }

    Vector3[][] GenerateFaceEdges(Face face)
    {
        Vector3[][] result = new Vector3[4][];

        result[0] = new Vector3[resolution + 1]; // bottom
        result[1] = new Vector3[resolution + 1]; // right
        result[2] = new Vector3[resolution + 1]; // top
        result[3] = new Vector3[resolution + 1]; // left

        float step = (quadWidthPercent * 2f) / resolution;

        // bottom (y = -1)
        for (int i = 0; i <= resolution; i++)
        {
            float px = -quadWidthPercent + i * step;
            result[0][i] = ToSphere(face, px, -quadWidthPercent);
        }

        // right (x = +1)
        for (int i = 0; i <= resolution; i++)
        {
            float py = -quadWidthPercent + i * step;
            result[1][i] = ToSphere(face, quadWidthPercent, py);
        }

        // top (y = +1)
        for (int i = 0; i <= resolution; i++)
        {
            float px = quadWidthPercent - i * step;
            result[2][i] = ToSphere(face, px, quadWidthPercent);
        }

        // left (x = -1)
        for (int i = 0; i <= resolution; i++)
        {
            float py = quadWidthPercent - i * step;
            result[3][i] = ToSphere(face, -quadWidthPercent, py);
        }

        return result;
    }

    // =========================================
    // LINK BUILDING
    // =========================================
    void BuildLinks()
    {
        Transform parent = transform.Find("NAV_LINKS");

        linkWidth = transform.lossyScale.x/resolution * temp;

        if (parent == null)
        {
            GameObject go = new GameObject("NAV_LINKS");
            go.transform.SetParent(transform);
            parent = go.transform;
        }

        // clear old
#if UNITY_EDITOR
        if (!Application.isPlaying)
            while (parent.childCount > 0)
                DestroyImmediate(parent.GetChild(0).gameObject);
        else
#endif
            while (parent.childCount > 0)
                Destroy(parent.GetChild(0).gameObject);

        // Define adjacency (faceA, edgeA, faceB, edgeB, reverse?)
        var connections = new (Face, int, Face, int, bool)[]
        {
            // +Y face (already working, kept consistent)
            (Face.PY, 0, Face.NZ, 2, false),
            (Face.PY, 1, Face.PX, 2, false),
            (Face.PY, 2, Face.PZ, 2, false),
            (Face.PY, 3, Face.NX, 2, false),

            // +Z face
            (Face.PZ, 0, Face.NY, 0, false),
            (Face.PZ, 1, Face.PX, 3, true),
            (Face.PZ, 3, Face.NX, 1, true),

            // -Y face
            (Face.NY, 1, Face.PX, 0, false),
            (Face.NY, 2, Face.NZ, 0, false),
            (Face.NY, 3, Face.NX, 0, false),

            // -Z face
            (Face.NZ, 1, Face.NX, 3, true),
            (Face.NZ, 3, Face.PX, 1, true),
        };

        foreach (var c in connections)
        {
            var a = edges[(int)c.Item1][c.Item2];
            var b = edges[(int)c.Item3][c.Item4];

            for (int i = 0; i <= resolution; i++)
            {
                int j = c.Item5 ? (resolution - i) : i;

                float half = resolution * 0.5f;
                float t = 1f - Mathf.Abs(i - half) / half;


                CreateLink(parent, a[i], b[j], t);
            }
        }
    }
    void CreateLink(Transform parent, Vector3 a, Vector3 b, float t)
    {
        GameObject go = new GameObject("NavLink");
        go.transform.SetParent(parent);

        var link = go.AddComponent<NavMeshLink>();

        Vector3 dir = (b - a);
        float length = dir.magnitude;

        Vector3 forward = dir.normalized;

        // Position at midpoint
        Vector3 mid = (a + b) * 0.5f;
        go.transform.position = mid;

        // 🔥 CRITICAL: align forward with link direction
        go.transform.rotation = Quaternion.LookRotation(forward, mid.normalized);

        // 🔥 CRITICAL: endpoints along local Z axis
        link.startPoint = new Vector3(0, 0, -length * 0.5f);
        link.endPoint = new Vector3(0, 0, length * 0.5f);

        float widthFactor = widthRamp.Evaluate(t);
        link.width = linkWidth * widthFactor;
    }

    // =========================================
    // MAPPING
    // =========================================
    Vector3 ToSphere(Face face, float px, float py)
    {
        Vector3 cube;

        switch (face)
        {
            case Face.PY: cube = new Vector3(px, 1f, py); break;
            case Face.NY: cube = new Vector3(px, -1f, -py); break;

            case Face.PX: cube = new Vector3(1f, py, -px); break;
            case Face.NX: cube = new Vector3(-1f, py, px); break;

            case Face.PZ: cube = new Vector3(px, py, 1f); break;
            case Face.NZ: cube = new Vector3(-px, py, -1f); break;

            default: cube = Vector3.zero; break;
        }

        cube.Normalize();
        return transform.position + cube * transform.lossyScale.x;
    }

    // void OnDrawGizmos()
    // {
    //     if (edges == null)
    //         return;

    //     Color[] faceColors =
    //     {
    //     Color.red,     // PX
    //     Color.blue,    // NX
    //     Color.green,   // PY
    //     Color.yellow,  // NY
    //     Color.cyan,    // PZ
    //     Color.magenta  // NZ
    // };

    //     for (int f = 0; f < edges.Length; f++)
    //     {
    //         if (edges[f] == null) continue;

    //         Gizmos.color = faceColors[f];

    //         for (int e = 0; e < 4; e++)
    //         {
    //             var edge = edges[f][e];
    //             if (edge == null) continue;

    //             for (int i = 0; i < edge.Length; i++)
    //             {
    //                 Gizmos.DrawSphere(edge[i], gizmoSize);
    //             }
    //         }
    //     }
    // }
}