using UnityEngine;

[System.Serializable]
public class SubvoxelDefinition
{
    public string key;
    public Mesh mesh;
}

public enum SubvoxelType
{
    None,
    _4C,
    _4S,
    _5C,
    _5S,
    _6C,
    _6S,
    _7C,
    _7CV,
    _7S,
    _7SV,
    _8F
}

