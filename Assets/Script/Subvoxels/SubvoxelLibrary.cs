using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[CreateAssetMenu(menuName = "Subvoxels/Library")]
public class SubvoxelLibrary : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string key;
        public Mesh mesh;
    }

    public List<Entry> entries = new List<Entry>();

    public Mesh Get(string key)
    {
        return entries.Find(e => e.key == key)?.mesh;
    }
}

public static class SubvoxelLibraryBuilder
{
    private const string LIB_PATH = "Assets/Data/SubvoxelLibrary.asset";
    private const string SEARCH_FOLDER = "Assets/Meshes/Subvoxels"; // <- your FBX folder

    [MenuItem("Tools/Subvoxels/Rebuild Library")]
    public static void Rebuild()
    {
        // Load or create library
        var lib = AssetDatabase.LoadAssetAtPath<SubvoxelLibrary>(LIB_PATH);

        if (lib == null)
        {
            lib = ScriptableObject.CreateInstance<SubvoxelLibrary>();
            AssetDatabase.CreateAsset(lib, LIB_PATH);
        }

        // Find all FBX files
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { SEARCH_FOLDER });

        int added = 0;
        int updated = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Load ALL assets inside FBX
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (var a in assets)
            {
                if (!(a is Mesh mesh)) continue;

                string key = mesh.name;

                if (string.IsNullOrEmpty(key))
                    continue;

                // Try find existing
                var existing = lib.entries.Find(e => e.key == key);

                if (existing != null)
                {
                    if (existing.mesh != mesh)
                    {
                        existing.mesh = mesh;
                        updated++;
                    }
                }
                else
                {
                    lib.entries.Add(new SubvoxelLibrary.Entry
                    {
                        key = key,
                        mesh = mesh
                    });

                    added++;
                }
            }
        }

        // Optional: remove dead entries
        lib.entries.RemoveAll(e => e.mesh == null);

        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();

        Debug.Log($"Subvoxel Library Rebuilt → Added: {added}, Updated: {updated}, Total: {lib.entries.Count}");
    }
}
