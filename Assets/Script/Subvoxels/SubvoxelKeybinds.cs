
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SubvoxelKeybind
{
    public SubvoxelType type;
    public KeyCode key;
}


[CreateAssetMenu(menuName = "Subvoxels/Keybinds")]
public class SubvoxelKeybinds : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;
        public KeyCode keybind;
    }

    public List<Entry> entries = new List<Entry>();
}

