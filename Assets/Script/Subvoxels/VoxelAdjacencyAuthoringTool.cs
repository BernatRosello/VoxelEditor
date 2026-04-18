using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public class VoxelAdjacencyAuthoringTool : EditorWindow
{
    [System.Serializable]
    public class Keybind
    {
        public KeyCode key;
        public int libraryIndex;
    }

    public SubvoxelLibrary library;
    public SubvoxelKeybinds keybindsAsset;
    public SubvoxelAdjacencyDatabase database;

    private List<string> libKeys = new List<string>();

    private List<(int center, int[] cfg)> cases = new List<(int, int[])>();
    private int index = 0;

    [System.Serializable]
    public struct PlacedSubvoxel
    {
        public string key;
        public Quaternion rotation;
        public int orientationIndex;
    }


    private PlacedSubvoxel[] placed = new PlacedSubvoxel[8];
    static readonly Quaternion[] ROTS = new Quaternion[8]
    {
        // 0 (-,-,-)
        Quaternion.Euler(180, 0, 90),

        // 1 (+,-,-)
        Quaternion.Euler(180, 0, 0),

        // 2 (-,+,-)
        Quaternion.Euler(180, 0, 180),

        // 3 (+,+,-)
        Quaternion.Euler(180, 0, -90),

        // 4 (-,-,+)
        Quaternion.Euler(0, 0, 180),

        // 5 (+,-,+)
        Quaternion.Euler(0, 0, -90),

        // 6 (-,+,+)
        Quaternion.Euler(0, 0, 90),

        // 7 (+,+,+) ← canonical
        Quaternion.identity
    };

    int activeOrientation = 0;

    private int previewIndex = 0;

    private static readonly Vector3[] DIRS = {
        new Vector3(0,0,1), new Vector3(0,0,-1),
        new Vector3(-1,0,0), new Vector3(1,0,0),
        new Vector3(0,1,0), new Vector3(0,-1,0)
    };

    private static readonly Color[] COLORS = {
        new Color(0,0,0,0),
        new Color(0.2f,0.4f,1f,0.4f),
        new Color(1f,0.5f,0.1f,1f),
        new Color(1f,0f,0.7f,1f)
    };

    Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

    private const float SUB_SIZE = 1f;

    [MenuItem("Tools/Subvoxels/Voxel Authoring")]
    public static void Open()
    {
        GetWindow<VoxelAdjacencyAuthoringTool>();
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        GenerateCases();
        RefreshLibrary();
        LoadCurrent();
    }

    Material GetPreviewMaterial(string key, bool preview = false)
    {
        string id = key + (preview ? "_preview" : "_solid");

        if (materialCache.TryGetValue(id, out var mat))
            return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");

        mat = new Material(shader);

        // color hashing → stable visual differentiation
        Color col = Color.HSVToRGB(
            Mathf.Abs(key.GetHashCode() % 1000) / 1000f,
            0.6f,
            1f
        );

        if (preview)
        {
            col.a = 0.3f;
            mat.SetFloat("_Surface", 1); // transparent (URP)
            mat.renderQueue = 3000;
        }

        mat.color = col;

        materialCache[id] = mat;
        return mat;
    }


    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void RefreshLibrary()
    {
        if (library == null) return;
        libKeys = library.entries.Select(e => e.key).ToList();
    }

    void OnGUI()
    {
        keybindsAsset = (SubvoxelKeybinds)EditorGUILayout.ObjectField(
            "keybindsAsset.entries Asset",
            keybindsAsset,
            typeof(SubvoxelKeybinds),
            false
        );

        if (keybindsAsset == null)
        {
            EditorGUILayout.HelpBox("Assign keybindsAsset.entries Asset", MessageType.Warning);
            return;
        }

        EditorGUI.BeginChangeCheck();
        library = (SubvoxelLibrary)EditorGUILayout.ObjectField("Library", library, typeof(SubvoxelLibrary), false);
        if (EditorGUI.EndChangeCheck())
            RefreshLibrary();

        if (library == null)
        {
            EditorGUILayout.HelpBox("Assign SubvoxelLibrary", MessageType.Warning);
            return;
        }

        database = (SubvoxelAdjacencyDatabase)EditorGUILayout.ObjectField(
                "Adjacency Database",
                database,
                typeof(SubvoxelAdjacencyDatabase),
                false
            );

        if (database == null)
        {
            EditorGUILayout.HelpBox("Assign Adjacency Database", MessageType.Warning);
        }


        GUILayout.Space(10);

        // keybinds
        var entries = keybindsAsset.entries;

        // auto sync size
        while (entries.Count < libKeys.Count)
            entries.Add(new SubvoxelKeybinds.Entry());

        while (entries.Count > libKeys.Count)
            entries.RemoveAt(entries.Count - 1);

        // enforce keys
        for (int i = 0; i < libKeys.Count; i++)
        {
            entries[i].key = libKeys[i];

            GUILayout.BeginHorizontal();

            GUILayout.Label(libKeys[i], GUILayout.Width(120));

            entries[i].keybind = (KeyCode)EditorGUILayout.EnumPopup(entries[i].keybind);

            GUILayout.EndHorizontal();
        }


        previewIndex = EditorGUILayout.Popup("Current Subvoxel", previewIndex, libKeys.ToArray());

        GUILayout.Space(10);

        if (cases == null || cases.Count == 0)
        {
            EditorGUILayout.HelpBox("Cases not initialized", MessageType.Error);
            return;
        }

        index = Mathf.Clamp(index, 0, cases.Count - 1);

        var (center, cfg) = cases[index];


        GUILayout.Label($"Case {index + 1}/{cases.Count}");
        GUILayout.Label($"Center: {(center == 2 ? "Smooth" : "Sharp")}");
        GUILayout.Label($"Adj: {string.Join(",", cfg)}");

        // ✅ OCTET VISUALIZATION
        GUILayout.Space(10);
        GUILayout.Label("Subvoxel Octet:");

        for (int z = 1; z >= 0; z--)
        {
            GUILayout.BeginHorizontal();
            for (int y = 1; y >= 0; y--)
            {
                GUILayout.BeginVertical();
                for (int x = 0; x < 2; x++)
                {
                    int i = x | (y << 1) | (z << 2);
                    string label = string.IsNullOrEmpty(placed[i].key) ?
                        $"{i}: -" :
                        $"{i}: {placed[i].key}\nR:{GetRotationIndex(placed[i].rotation)}";


                    GUI.backgroundColor = (i == NextSlot()) ? Color.yellow : Color.gray;
                    GUILayout.Box(label, GUILayout.Width(60));
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Prev")) Prev();
        if (GUILayout.Button("Next")) Next();
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Clear")) Clear();

        if (GUI.changed)
        {
            LoadCurrent();
            EditorUtility.SetDirty(keybindsAsset);
        }
    }

    void OnSceneGUI(SceneView sv)
    {
        if (library == null) return;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        var (center, cfg) = cases[index];

        DrawCenter(center);
        DrawNeighbors(cfg);
        DrawPlaced();
        DrawPreview();
        DrawSlotHighlight(); // ✅ NEW

        HandleInput(Event.current);

        SceneView.RepaintAll();
    }

    // -----------------------
    // DRAW
    // -----------------------

    // ✅ CENTER = OUTLINE ONLY
    void DrawCenter(int type)
    {
        Handles.color = COLORS[type];
        Handles.DrawWireCube(Vector3.zero, Vector3.one);
    }

    void DrawNeighbors(int[] cfg)
    {
        for (int i = 0; i < 6; i++)
        {
            int t = cfg[i];
            if (t == 0) continue;

            Handles.color = COLORS[t];
            //Handles.DrawWireCube(DIRS[i], Vector3.one);
            Handles.CubeHandleCap(0, DIRS[i], Quaternion.identity, 1f, EventType.Repaint);
        }
    }

    void DrawPlaced()
    {
        for (int i = 0; i < 8; i++)
        {
            if (string.IsNullOrEmpty(placed[i].key)) continue;

            Mesh mesh = library.Get(placed[i].key);
            if (!mesh) continue;

            var mat = GetPreviewMaterial(placed[i].key);

            Graphics.DrawMesh(
                mesh,
                Matrix4x4.TRS(
                    SlotPos(i),
                    placed[i].rotation,
                    Vector3.one * SUB_SIZE
                ),
                mat,
                0
            );
        }
    }

    void DrawPreview()
    {
        if (previewIndex >= libKeys.Count) return;

        int slot = NextSlot();
        if (slot == -1) return;

        string key = libKeys[previewIndex];
        Mesh mesh = library.Get(key);
        if (!mesh) return;

        var mat = GetPreviewMaterial(key, true);

        Graphics.DrawMesh(
            mesh,
            Matrix4x4.TRS(
                SlotPos(slot),
                ROTS[activeOrientation],
                Vector3.one * SUB_SIZE
            ),
            mat,
            0
        );
    }

    // ✅ SLOT HIGHLIGHT (BOUNDING BOX ONLY)
    void DrawSlotHighlight()
    {
        int slot = NextSlot();
        if (slot == -1) return;

        Handles.color = Color.yellow;

        Vector3 pos = SlotPos(slot);
        Handles.DrawWireCube(pos, Vector3.one * 0.5f);
    }

    // -----------------------
    // INPUT
    // -----------------------
    void HandleInput(Event e)
    {
        if (e.type != EventType.KeyDown) return;

        if (EditorWindow.focusedWindow != SceneView.lastActiveSceneView)
            return;

        if (e.keyCode == KeyCode.UpArrow)
        {
            activeOrientation = (activeOrientation + 1) % ROTS.Length;
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.DownArrow)
        {
            activeOrientation = (activeOrientation - 1 + ROTS.Length) % ROTS.Length;
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.PageDown)
        {
            Next();
            return;
        }

        if (e.keyCode == KeyCode.PageUp)
        {
            Prev();
            return;
        }

        if (e.keyCode == KeyCode.Backspace)
        {
            RemoveLast();
            e.Use();
            return;
        }

        foreach (var kb in keybindsAsset.entries)
        {
            if (e.keyCode == kb.keybind)
            {
                Place(kb.key);
                e.Use();
                return;
            }
        }

        if (e.keyCode == KeyCode.Return)
        {
            SaveCurrent();
            e.Use();
            return;
        }

    }

    void Place(string key)
    {
        int slot = NextSlot();
        if (slot == -1) return;

        placed[slot] = new PlacedSubvoxel
        {
            key = key,
            rotation = ROTS[activeOrientation]
        };

        if (NextSlot() != -1)
            activeOrientation = NextSlot();
        else
            activeOrientation = 0;
    }


    int NextSlot()
    {
        for (int i = 0; i < 8; i++)
            if (string.IsNullOrEmpty(placed[i].key))
                return i;
        return -1;
    }


    void RemoveLast()
    {
        for (int i = 7; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(placed[i].key))
            {
                placed[i] = default;
                return;
            }
        }
    }


    void Clear()
    {
        for (int i = 0; i < 8; i++)
            placed[i] = default;
    }


    void Next()
    {
        index = (index + 1) % cases.Count;
        LoadCurrent();
    }

    void Prev()
    {
        index = (index - 1 + cases.Count) % cases.Count;
        LoadCurrent();
    }

    Vector3 SlotPos(int i)
    {
        return new Vector3(
            (i & 1) == 0 ? -0.25f : 0.25f,
            (i & 2) == 0 ? -0.25f : 0.25f,
            (i & 4) == 0 ? -0.25f : 0.25f
        );
    }

    void GenerateCases()
    {
        cases.Clear();

        var rotations = GenerateRotations();
        Dictionary<string, int[]> unique = new Dictionary<string, int[]>();

        // FULL STATE SPACE (0..3 per face, like python)
        int[] cfg = new int[6];

        void Recurse(int depth)
        {
            if (depth == 6)
            {
                var canon = Canonical(cfg, rotations);
                string key = string.Join("", canon);

                if (!unique.ContainsKey(key))
                    unique[key] = (int[])cfg.Clone();

                return;
            }

            for (int v = 0; v < 4; v++) // 0,1,2,3
            {
                cfg[depth] = v;
                Recurse(depth + 1);
            }
        }

        Recurse(0);

        // assign center types
        foreach (var u in unique.Values)
        {
            cases.Add((2, u)); // smooth
            cases.Add((3, u)); // sharp
        }
    }
    List<Quaternion> GenerateRotations()
    {
        List<Vector3> axes = new List<Vector3>
    {
        Vector3.right, Vector3.up, Vector3.forward,
        -Vector3.right, -Vector3.up, -Vector3.forward
    };

        List<Quaternion> rots = new List<Quaternion>();

        foreach (var x in axes)
        {
            foreach (var y in axes)
            {
                if (Mathf.Abs(Vector3.Dot(x, y)) > 0.9f) continue;

                var z = Vector3.Cross(x, y);
                if (z.magnitude < 0.9f) continue;

                Matrix4x4 m = new Matrix4x4();
                m.SetColumn(0, x);
                m.SetColumn(1, y);
                m.SetColumn(2, z);
                m.SetColumn(3, new Vector4(0, 0, 0, 1));

                if (m.determinant < 0) continue;

                Quaternion q = m.rotation;

                bool exists = rots.Any(r => Quaternion.Angle(r, q) < 0.1f);
                if (!exists)
                    rots.Add(q);
            }
        }

        return rots;
    }
    int[] RotateConfig(int[] cfg, Quaternion rot)
    {
        int[] result = new int[6];

        for (int i = 0; i < 6; i++)
        {
            Vector3 rv = rot * DIRS[i];

            int best = 0;
            float bestDot = -999f;

            for (int j = 0; j < 6; j++)
            {
                float d = Vector3.Dot(rv, DIRS[j]);
                if (d > bestDot)
                {
                    bestDot = d;
                    best = j;
                }
            }

            result[best] = cfg[i];
        }

        return result;
    }
    int[] Canonical(int[] cfg, List<Quaternion> rots)
    {
        int[] best = null;
        string bestKey = null;

        foreach (var r in rots)
        {
            var rotated = RotateConfig(cfg, r);
            string key = string.Join("", rotated);

            if (bestKey == null || string.Compare(key, bestKey) < 0)
            {
                bestKey = key;
                best = rotated;
            }
        }

        return best;
    }


    int GetRotationIndex(Quaternion q)
    {
        for (int i = 0; i < ROTS.Length; i++)
        {
            if (Quaternion.Angle(q, ROTS[i]) < 0.1f)
                return i;
        }
        return 0;
    }
    void SaveCurrent()
    {
        if (database == null) return;

        var (center, cfg) = cases[index];

        // ensure full octet
        for (int i = 0; i < 8; i++)
            if (string.IsNullOrEmpty(placed[i].key))
                return;

        var entry = database.Get(center, cfg);

        if (entry == null)
        {
            entry = new SubvoxelAdjacencyDatabase.Entry();
            entry.center = center;
            entry.cfg = (int[])cfg.Clone();
            database.entries.Add(entry);
        }

        entry.keys = new string[8];
        entry.orientations = new int[8];

        for (int i = 0; i < 8; i++)
        {
            entry.keys[i] = placed[i].key;
            entry.orientations[i] = GetRotationIndex(placed[i].rotation);
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }

    void LoadCurrent()
    {
        Clear();

        if (database == null) return;

        var (center, cfg) = cases[index];
        var entry = database.Get(center, cfg);

        if (entry == null) return;

        for (int i = 0; i < 8; i++)
        {
            if (entry.keys == null || i >= entry.keys.Length) continue;
            if (string.IsNullOrEmpty(entry.keys[i])) continue;

            int rotIndex = 0;

            if (entry.orientations != null && i < entry.orientations.Length)
                rotIndex = Mathf.Clamp(entry.orientations[i], 0, ROTS.Length - 1);

            placed[i] = new PlacedSubvoxel
            {
                key = entry.keys[i],
                rotation = ROTS[rotIndex]
            };
        }

        // optional: restore orientation cursor to next slot
        int next = NextSlot();
        activeOrientation = next != -1 ? next : 0;
    }


}
