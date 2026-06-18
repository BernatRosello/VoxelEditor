using UnityEngine;
using UnityEditor;
using System.IO;

public class SphereQuadrantMeshCreator
{
    [MenuItem("Tools/Create/Sphere Quadrant Mesh")]
    public static void CreateMesh()
    {
        int resolution = 32;
        float radius = 1f;

        Mesh mesh = GenerateSphereQuadrant(resolution, radius);

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Sphere Quadrant Mesh",
            "SphereQuadrant.asset",
            "asset",
            "Choose location"
        );

        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Create preview GameObject
            GameObject go = new GameObject("SphereQuadrant_Preview");
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();

            mf.sharedMesh = mesh;

            // Try to get a default lit shader (URP / Built-in fallback)
            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.name = "DefaultLitPreviewMaterial";

            mr.sharedMaterial = mat;

            // Focus in scene
            Selection.activeGameObject = go;
            SceneView.lastActiveSceneView.FrameSelected();
        }
    }


    static Mesh GenerateSphereQuadrant(int resolution, float radius)
    {
        Mesh mesh = new Mesh();
        mesh.name = "SphereQuadrant";

        int vertCountPerSide = resolution + 1;
        int vertexCount = vertCountPerSide * vertCountPerSide;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[resolution * resolution * 6];

        int v = 0;
        int t = 0;

        // Build vertices
        for (int y = 0; y <= resolution; y++)
        {
            float fy = (float)y / resolution;

            for (int x = 0; x <= resolution; x++)
            {
                float fx = (float)x / resolution;

                // Map to cube face [-1, 1]
                float px = Mathf.Lerp(-1f, 1f, fx);
                float py = Mathf.Lerp(-1f, 1f, fy);

                // Choose cube face: +Y face
                Vector3 cubePos = new Vector3(px, 1f, py);

                // Project onto sphere
                Vector3 spherePos = cubePos.normalized * radius;

                vertices[v] = spherePos;
                normals[v] = spherePos.normalized;
                uvs[v] = new Vector2(fx, fy);

                // Build triangles
                if (x < resolution && y < resolution)
                {
                    int i = v;
                    int iRight = v + 1;
                    int iUp = v + vertCountPerSide;
                    int iUpRight = iUp + 1;

                    // Triangle 1
                    triangles[t++] = i;
                    triangles[t++] = iUp;
                    triangles[t++] = iRight;

                    // Triangle 2
                    triangles[t++] = iRight;
                    triangles[t++] = iUp;
                    triangles[t++] = iUpRight;
                }

                v++;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();

        return mesh;
    }
}
