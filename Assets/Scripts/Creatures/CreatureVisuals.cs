using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CreatureVisuals", menuName = "CreatureVisuals", order = 0)]
public class CreatureVisuals : ScriptableObject
{
    [Header("Head")]
    public HeadBlendShapes headBlendShapes;
    public Color headColor;
    public MaterialOption headMaterial;

    [Header("Eyes")]
    public EyesBlendShapes eyesBlendShapes;
    public Color eyesColor;
    public MaterialOption eyesMaterial;

    [Header("Torso")]
    public TorsoBlendShapes torsoBlendShapes;
    public Color torsoColor;
    public MaterialOption torsoMaterial;

    [Header("Arms")]
    public ArmsBlendShapes armsBlendShapes;
    public Color armsColor;
    public MaterialOption armsMaterial;

    [Header("Legs")]
    public LegsBlendShapes legsBlendShapes;
    public Color legsColor;
    public MaterialOption legsMaterial;

    public static void SetMaterials(BodyMesh body, CreatureVisuals visuals)
    {
        SetHeadMaterial(body, visuals);
        SetEyesMaterial(body, visuals);
        SetTorsoMaterial(body, visuals);
        SetArmsMaterial(body, visuals);
        SetLegsMaterial(body, visuals);
    }

    public static void SetColors(BodyMesh body, CreatureVisuals visuals)
    {
        SetHeadColor(body, visuals);
        SetEyesColor(body, visuals);
        SetTorsoColor(body, visuals);
        SetArmsColor(body, visuals);
        SetLegsColor(body, visuals);
    }
    
    public static void SetBlendShapeWeights(BodyMesh body, CreatureVisuals visuals)
    {
        SetHeadBlendShapeWeights(body, visuals);
        SetEyesBlendShapeWeights(body, visuals);
        SetTorsoBlendShapeWeights(body, visuals);
        SetArmsBlendShapeWeights(body, visuals);
        SetLegsBlendShapeWeights(body, visuals);
    }

    public static void SetHeadMaterial(BodyMesh body, CreatureVisuals visuals)
    {
        body.Head.sharedMaterial = CreatureEditorManager.GetMaterial(visuals.headMaterial);
    }
    public static void SetEyesMaterial(BodyMesh body, CreatureVisuals visuals)
    {
        body.Eyes.sharedMaterial = CreatureEditorManager.GetMaterial(visuals.eyesMaterial);
    }
    public static void SetTorsoMaterial(BodyMesh body, CreatureVisuals visuals)
    {
        body.Torso.sharedMaterial = CreatureEditorManager.GetMaterial(visuals.torsoMaterial);
    }
    public static void SetArmsMaterial(BodyMesh body, CreatureVisuals visuals)
    {
        body.Arms.sharedMaterial = CreatureEditorManager.GetMaterial(visuals.armsMaterial);
    }
    public static void SetLegsMaterial(BodyMesh body, CreatureVisuals visuals)
    {
        body.Legs.sharedMaterial = CreatureEditorManager.GetMaterial(visuals.legsMaterial);
    }


    public static void SetHeadColor(BodyMesh body, CreatureVisuals visuals)
    {
        Color[] newColors = new Color[body.Head.sharedMesh.vertexCount];
        for (int i = 0; i < newColors.Length; i++) { newColors[i] = visuals.headColor; }
        body.Head.sharedMesh.colors = newColors;
    }
    public static void SetEyesColor(BodyMesh body, CreatureVisuals visuals)
    {
        Color[] newColors = new Color[body.Eyes.sharedMesh.vertexCount];
        for (int i = 0; i < newColors.Length; i++) { newColors[i] = visuals.eyesColor; }
        body.Eyes.sharedMesh.colors = newColors;
    }
    public static void SetTorsoColor(BodyMesh body, CreatureVisuals visuals)
    {
        Color[] newColors = new Color[body.Torso.sharedMesh.vertexCount];
        for (int i = 0; i < newColors.Length; i++) { newColors[i] = visuals.torsoColor; }
        body.Torso.sharedMesh.colors = newColors;
    }
    public static void SetArmsColor(BodyMesh body, CreatureVisuals visuals)
    {
        Color[] newColors = new Color[body.Arms.sharedMesh.vertexCount];
        for (int i = 0; i < newColors.Length; i++) { newColors[i] = visuals.armsColor; }
        body.Arms.sharedMesh.colors = newColors;
    }
    public static void SetLegsColor(BodyMesh body, CreatureVisuals visuals)
    {
        Color[] newColors = new Color[body.Legs.sharedMesh.vertexCount];
        for (int i = 0; i < newColors.Length; i++) { newColors[i] = visuals.legsColor; }
        body.Legs.sharedMesh.colors = newColors;
    }


    private static void SetLegsBlendShapeWeights(BodyMesh body, CreatureVisuals visuals)
    {
        body.Legs.SetBlendShapeWeight(LegsBlendShapes.CylinderIndex, visuals.legsBlendShapes.Cylinder);
        body.Legs.SetBlendShapeWeight(LegsBlendShapes.TaperIndex, visuals.legsBlendShapes.Taper);
    }

    private static void SetArmsBlendShapeWeights(BodyMesh body, CreatureVisuals visuals)
    {
        body.Arms.SetBlendShapeWeight(ArmsBlendShapes.CylinderIndex, visuals.armsBlendShapes.Cylinder);
        body.Arms.SetBlendShapeWeight(ArmsBlendShapes.TaperIndex, visuals.armsBlendShapes.Taper);
    }

    private static void SetTorsoBlendShapeWeights(BodyMesh body, CreatureVisuals visuals)
    {
        body.Torso.SetBlendShapeWeight(TorsoBlendShapes.CylinderIndex, visuals.torsoBlendShapes.Cylinder);
        body.Torso.SetBlendShapeWeight(TorsoBlendShapes.TaperIndex, visuals.torsoBlendShapes.Taper);
    }

    private static void SetEyesBlendShapeWeights(BodyMesh body, CreatureVisuals visuals)
    {
        body.Eyes.SetBlendShapeWeight(EyesBlendShapes.SizeIndex, visuals.eyesBlendShapes.Size);
        body.Eyes.SetBlendShapeWeight(EyesBlendShapes.DistanceIndex, visuals.eyesBlendShapes.Distance);
        body.Eyes.SetBlendShapeWeight(EyesBlendShapes.CylinderIndex, visuals.eyesBlendShapes.Cylinder);
        body.Eyes.SetBlendShapeWeight(EyesBlendShapes.BoreIndex, visuals.eyesBlendShapes.Bore);
    }

    public static void SetHeadBlendShapeWeights(BodyMesh body, CreatureVisuals visuals)
    {
        body.Head.SetBlendShapeWeight(HeadBlendShapes.FloatIndex, visuals.headBlendShapes.Float);
        body.Head.SetBlendShapeWeight(HeadBlendShapes.ShrinkIndex, visuals.headBlendShapes.Shrink);
        body.Head.SetBlendShapeWeight(HeadBlendShapes.CylinderIndex, visuals.headBlendShapes.Cylinder);
        body.Head.SetBlendShapeWeight(HeadBlendShapes.SphereIndex, visuals.headBlendShapes.Sphere);
        body.Head.SetBlendShapeWeight(HeadBlendShapes.TaperIndex, visuals.headBlendShapes.Taper);
        body.Head.SetBlendShapeWeight(HeadBlendShapes.PointyEarsIndex, visuals.headBlendShapes.PointyEars);
        body.Head.SetBlendShapeWeight(HeadBlendShapes.DroopyEarsIndex, visuals.headBlendShapes.DroopyEars);
        body.Head.SetBlendShapeWeight(HeadBlendShapes.UnicornIndex, visuals.headBlendShapes.Unicorn);
        body.Head.SetBlendShapeWeight(HeadBlendShapes.SpikesIndex, visuals.headBlendShapes.Spikes);
        body.Head.SetBlendShapeWeight(HeadBlendShapes.MohawkIndex, visuals.headBlendShapes.Mohawk);
    }
    

    [System.Serializable]
    public enum MaterialOption
    {
        Fur,
        BSDF
    }

    [System.Serializable]
    public struct HeadBlendShapes
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

        public const int FloatIndex = 0;
        public const int ShrinkIndex = 1;
        public const int CylinderIndex = 2;
        public const int SphereIndex = 3;
        public const int TaperIndex = 4;
        public const int PointyEarsIndex = 5;
        public const int DroopyEarsIndex = 6;
        public const int UnicornIndex = 7;
        public const int SpikesIndex = 8;
        public const int MohawkIndex = 9;
    }

    [System.Serializable]
    public struct EyesBlendShapes
    {
        [Range(-100f, 200f)]
        public float Size;
        [Range(-50f, 80f)]
        public float Distance;
        [Range(-200f, 200f)]
        public float Cylinder;
        [Range(-100f, 100f)]
        public float Bore;

        public const int SizeIndex = 0;
        public const int DistanceIndex = 1;
        public const int CylinderIndex = 2;
        public const int BoreIndex = 3;
    }

    [System.Serializable]
    public struct TorsoBlendShapes
    {
        [Range(-100f, 200f)]
        public float Cylinder;
        [Range(-100f, 100f)]
        public float Taper;

        public const int CylinderIndex = 0;
        public const int TaperIndex = 1;
    }

    [System.Serializable]
    public struct ArmsBlendShapes
    {
        [Range(-100f, 100f)]
        public float Cylinder;
        [Range(-75f, 100f)]
        public float Taper;

        public const int CylinderIndex = 0;
        public const int TaperIndex = 1;
    }

    [System.Serializable]
    public struct LegsBlendShapes
    {
        [Range(-100f, 100f)]
        public float Cylinder;
        [Range(-200f, 100f)]
        public float Taper;

        public const int CylinderIndex = 0;
        public const int TaperIndex = 1;
    }
}