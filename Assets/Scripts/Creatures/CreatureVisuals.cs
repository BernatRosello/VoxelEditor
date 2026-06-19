using UnityEngine;

[CreateAssetMenu(fileName = "CreatureVisuals", menuName = "CreatureVisuals", order = 0)]
public class CreatureVisuals : ScriptableObject
{
    [Header("Head")]
    public HeadBlendShapes headBlendShapes;
    public Color headColor;

    [Header("Eyes")]
    public EyesBlendShapes eyesBlendShapes;
    public Color eyesColor;
    
    [Header("Torso")]
    public TorsoBlendShapes torsoBlendShapes;
    public Color torsoColor;

    [Header("Arms")]
    public ArmsBlendShapes armsBlendShapes;
    public Color armsColor;
    
    [Header("Legs")]
    public LegsBlendShapes legsBlendShapes;
    public Color legsColor;
}

[System.Serializable]
public sealed class HeadBlendShapes
{
    [Range(-100f, 100f)]
    public float Float;
    [Range(-100f, 100f)]
    public float Shrink;
    [Range(-100f, 100f)]
    public float Cylinder;
    [Range(-100f, 100f)]
    public float Sphere;
    [Range(-100f, 100f)]
    public float Taper;
    [Range(-100f, 100f)]
    public float PointyEars;
    [Range(-100f, 100f)]
    public float DroopyEars;
    [Range(-100f, 100f)]
    public float Unicorn;
    [Range(-100f, 100f)]
    public float Spikes;
    [Range(-100f, 100f)]
    public float Mohawk;
}

[System.Serializable]
public sealed class EyesBlendShapes
{
    [Range(-100f, 200f)]
    public float Size;
    [Range(-50f, 80f)]
    public float Distance;
    [Range(-200f, 200f)]
    public float Cylinder;
    [Range(-100f, 100f)]
    public float Bore;
}

[System.Serializable]
public class TorsoBlendShapes
{
    [Range(-100f, 200f)]
    public float Cylinder;
    [Range(-100f, 100f)]
    public float Taper;
}

[System.Serializable]
public class ArmsBlendShapes
{
    [Range(-100f, 100f)]
    public float Cylinder;
    [Range(-75f, 100f)]
    public float Taper;
}

[System.Serializable]
public class LegsBlendShapes
{
    [Range(-100f, 100f)]
    public float Cylinder;
    [Range(-200f, 100f)]
    public float Taper;
}