using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class QuadSphereEdges : MonoBehaviour
{
    public float radius = 1f;
    public int resolution = 16;

    public float gizmoSize = 0.04f;

    private List<Vector3> edgePoints = new List<Vector3>();

    enum Face { PX, NX, PY, NY, PZ, NZ }

    public void RebuildLinks()
    {
        edgePoints.Clear();

        foreach (Face face in System.Enum.GetValues(typeof(Face)))
        {
            GenerateFaceEdges(face);
        }
    }

    // =========================================
    // GENERATE EDGES (MATCHES YOUR MESH CODE)
    // =========================================
    void GenerateFaceEdges(Face face)
    {
        for (int y = 0; y <= resolution; y++)
        {
            float fy = (float)y / resolution;
            float py = Mathf.Lerp(-1f, 1f, fy);

            for (int x = 0; x <= resolution; x++)
            {
                // Only edge vertices
                if (x != 0 && x != resolution && y != 0 && y != resolution)
                    continue;

                float fx = (float)x / resolution;
                float px = Mathf.Lerp(-1f, 1f, fx);

                Vector3 cubePos = CubeFaceToXYZ(face, px, py);
                Vector3 spherePos = cubePos.normalized * radius;

                edgePoints.Add(spherePos);
            }
        }
    }

    // =========================================
    // EXACT SAME FACE MAPPING STYLE
    // =========================================
    Vector3 CubeFaceToXYZ(Face face, float px, float py)
    {
        switch (face)
        {
            case Face.PY: return new Vector3(px, 1f, py);   // your example
            case Face.NY: return new Vector3(px, -1f, -py);

            case Face.PX: return new Vector3(1f, py, -px);
            case Face.NX: return new Vector3(-1f, py, px);

            case Face.PZ: return new Vector3(px, py, 1f);
            case Face.NZ: return new Vector3(-px, py, -1f);
        }

        return Vector3.zero;
    }

    // =========================================
    // DEBUG
    // =========================================
    void OnDrawGizmos()
    {
        if (edgePoints == null) return;

        Gizmos.color = Color.white;

        foreach (var p in edgePoints)
        {
            Gizmos.DrawSphere(transform.position + p, gizmoSize);
        }
    }
}