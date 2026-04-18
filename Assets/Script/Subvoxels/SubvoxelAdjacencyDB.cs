using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Subvoxels/Adjacency Database")]
public class SubvoxelAdjacencyDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public int center;
        public int[] cfg = new int[6];
        public string[] keys = new string[8];
        public int[] orientations = new int[8];
    }

    public List<Entry> entries = new List<Entry>();

    public Entry Get(int center, int[] cfg)
    {
        foreach (var e in entries)
        {
            if (e.center != center) continue;

            bool match = true;
            for (int i = 0; i < 6; i++)
                if (e.cfg[i] != cfg[i]) match = false;

            if (match) return e;
        }
        return null;
    }
}
