using System;
using Unity.Android.Gradle;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "CreatureVisuals", menuName = "CreatureVisuals", order = 0)]
public class CreatureVisuals : ScriptableObject
{
    [Header("Head")]
    public HeadBlendShapes headBlendShapes;
    public Color headColor;
    public MaterialSettings headMaterial;

    [Header("Eyes")]
    public EyesBlendShapes eyesBlendShapes;
    public Color eyesColor;
    public MaterialSettings eyesMaterial;

    [Header("Torso")]
    public TorsoBlendShapes torsoBlendShapes;
    public Color torsoColor;
    public MaterialSettings torsoMaterial;

    [Header("Arms")]
    public ArmsBlendShapes armsBlendShapes;
    public Color armsColor;
    public MaterialSettings armsMaterial;

    [Header("Legs")]
    public LegsBlendShapes legsBlendShapes;
    public Color legsColor;
    public MaterialSettings legsMaterial;


    internal void SetMaterialSettings(BodyPart bodyPart, MaterialSettings value)
    {
        switch (bodyPart)
        {
            case BodyPart.Head:
                headMaterial = value;
                break;
            case BodyPart.Eyes:
                eyesMaterial = value;
                break;
            case BodyPart.Torso:
                torsoMaterial = value;
                break;
            case BodyPart.Arms:
                armsMaterial = value;
                break;
            case BodyPart.Legs:
                legsMaterial = value;
                break;
            default:
                return;
        }
    }
    internal MaterialSettings GetMaterialSettings(BodyPart bodyPart)
    {
        switch (bodyPart)
        {
            case BodyPart.Head:
                return headMaterial;
            case BodyPart.Eyes:
                return eyesMaterial;
            case BodyPart.Torso:
                return torsoMaterial;
            case BodyPart.Arms:
                return armsMaterial;
            case BodyPart.Legs:
                return legsMaterial;
            default:
                return default;
        }
    }

    internal void SetColor(BodyPart bodyPart, Color color)
    {
        switch (bodyPart)
        {
            case BodyPart.Head:
                headColor = color;
                break;
            case BodyPart.Eyes:
                eyesColor = color;
                break;
            case BodyPart.Torso:
                torsoColor = color;
                break;
            case BodyPart.Arms:
                armsColor = color;
                break;
            case BodyPart.Legs:
                legsColor = color;
                break;
            default:
                return;
        }
    }
    internal Color GetColor(BodyPart bodyPart)
    {
        switch (bodyPart)
        {
            case BodyPart.Head:
                return headColor;
            case BodyPart.Eyes:
                return eyesColor;
            case BodyPart.Torso:
                return torsoColor;
            case BodyPart.Arms:
                return armsColor;
            case BodyPart.Legs:
                return legsColor;
            default:
                return default;
        }
    }

    public object GetBlendShapeStruct(BodyPart bodyPart)
    {
        switch (bodyPart)
        {
            case BodyPart.Head:
                return headBlendShapes;
            case BodyPart.Eyes:
                return eyesBlendShapes;
            case BodyPart.Torso:
                return torsoBlendShapes;
            case BodyPart.Arms:
                return armsBlendShapes;
            case BodyPart.Legs:
                return legsBlendShapes;
            default:
                return null;
        }
    }

    public void SetBlendShapeStruct(BodyPart bodyPart, object value)
    {
        switch (bodyPart)
        {
            case BodyPart.Head:
                headBlendShapes = (HeadBlendShapes)value;
                break;
            case BodyPart.Eyes:
                eyesBlendShapes = (EyesBlendShapes)value;
                break;
            case BodyPart.Torso:
                torsoBlendShapes = (TorsoBlendShapes)value;
                break;
            case BodyPart.Arms:
                armsBlendShapes = (ArmsBlendShapes)value;
                break;
            case BodyPart.Legs:
                legsBlendShapes = (LegsBlendShapes)value;
                break;
            default:
                return;
        }
    }

    public static void ApplyMaterials(BodyMesh body, CreatureVisuals visuals)
    {
        ApplyHeadMaterial(body, visuals);
        ApplyEyesMaterial(body, visuals);
        ApplyTorsoMaterial(body, visuals);
        ApplyArmsMaterial(body, visuals);
        ApplyLegsMaterial(body, visuals);
    }

    public static void ApplyColors(BodyMesh body, CreatureVisuals visuals)
    {
        ApplyHeadColor(body, visuals);
        ApplyEyesColor(body, visuals);
        ApplyTorsoColor(body, visuals);
        ApplyArmsColor(body, visuals);
        ApplyLegsColor(body, visuals);
    }

    public static void ApplyBlendShapeWeights(BodyMesh body, CreatureVisuals visuals)
    {
        ApplyHeadBlendShapeWeights(body, visuals);
        ApplyEyesBlendShapeWeights(body, visuals);
        ApplyTorsoBlendShapeWeights(body, visuals);
        ApplyArmsBlendShapeWeights(body, visuals);
        ApplyLegsBlendShapeWeights(body, visuals);
    }

    public static void ApplyHeadMaterial(BodyMesh body, CreatureVisuals visuals)
    {
        ApplyAllMaterialProperties(body.Head, visuals.headColor, visuals.headMaterial);
    }
    public static void ApplyEyesMaterial(BodyMesh body, CreatureVisuals visuals)
    {
        ApplyAllMaterialProperties(body.Eyes, visuals.eyesColor, visuals.eyesMaterial);
    }
    public static void ApplyTorsoMaterial(BodyMesh body, CreatureVisuals visuals)
    {
        ApplyAllMaterialProperties(body.Torso, visuals.torsoColor, visuals.torsoMaterial);
    }
    public static void ApplyArmsMaterial(BodyMesh body, CreatureVisuals visuals)
    {
        ApplyAllMaterialProperties(body.Arms, visuals.armsColor, visuals.armsMaterial);
    }
    public static void ApplyLegsMaterial(BodyMesh body, CreatureVisuals visuals)
    {
        ApplyAllMaterialProperties(body.Legs, visuals.legsColor, visuals.legsMaterial);
    }

    static MaterialPropertyBlock materialPropertyBlock;
    public static void InitMaterialPropertyBlock()
    {
        if (materialPropertyBlock == null)
            materialPropertyBlock = new MaterialPropertyBlock();
    }
    public static void ApplyHeadColor(BodyMesh body, CreatureVisuals visuals)
    {
        ApplyColor(body.Head, visuals.headColor);
    }
    public static void ApplyEyesColor(BodyMesh body, CreatureVisuals visuals)
    {
        ApplyColor(body.Eyes, visuals.eyesColor);
    }
    public static void ApplyTorsoColor(BodyMesh body, CreatureVisuals visuals)
    {
        ApplyColor(body.Torso, visuals.torsoColor);
    }
    public static void ApplyArmsColor(BodyMesh body, CreatureVisuals visuals)
    {
        ApplyColor(body.Arms, visuals.armsColor);
    }
    public static void ApplyLegsColor(BodyMesh body, CreatureVisuals visuals)
    {
        ApplyColor(body.Legs, visuals.legsColor);
    }

    private static void ApplyColor(Renderer renderer, Color color)
    {
        InitMaterialPropertyBlock();
        renderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor("_BaseColor", color);
        renderer.SetPropertyBlock(materialPropertyBlock);
    }

    private static void ApplyAllMaterialProperties(Renderer renderer, Color color, MaterialSettings material)
    {
        InitMaterialPropertyBlock();

        renderer.sharedMaterial = CreatureEditorManager.GetMaterial(material.type);
        renderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor("_BaseColor", color);

        switch (material.type)
        {
            case MaterialOption.Fur:
                ApplyFurProperties(material.furSettings, materialPropertyBlock);
                break;
            case MaterialOption.OpaqueBSDF:
                ApplyBSDFProperties(material.bsdfSettings, materialPropertyBlock);
                break;
            case MaterialOption.TransparentBSDF:
                ApplyBSDFProperties(material.bsdfSettings, materialPropertyBlock);
                break;
        }

        renderer.SetPropertyBlock(materialPropertyBlock);
    }

    private static void ApplyBSDFProperties(BSDFSettings settings, MaterialPropertyBlock block)
    {
        block.SetFloat("_Metallic", settings.Metallic);
        block.SetFloat("_Smoothness", settings.Smoothness);
        block.SetFloat("_Emission", settings.Emission);
        if (settings.Opaqueness < 0.99f)
        {
            block.SetFloat("_Opaque", settings.Opaqueness);
        } else
        {
            
        }
    }

    private static void ApplyFurProperties(FurSettings settings, MaterialPropertyBlock block)
    {
        block.SetFloat("_Metallic", settings.Metallic);
        block.SetFloat("_PatternAmount", settings.Pattern);
        block.SetFloat("_Smoothness", settings.Smoothness);
        block.SetFloat("_ShellStep", settings.FurLength);
        block.SetFloat("_FurScale", settings.FurDensity);
        block.SetFloat("_AlphaCutout", settings.FurThickness);
    }


    private static void ApplyLegsBlendShapeWeights(BodyMesh body, CreatureVisuals visuals)
    {
        body.Legs.SetBlendShapeWeight(LegsBlendShapes.CylinderIndex, visuals.legsBlendShapes.Cylinder);
        body.Legs.SetBlendShapeWeight(LegsBlendShapes.TaperIndex, visuals.legsBlendShapes.Taper);
    }

    private static void ApplyArmsBlendShapeWeights(BodyMesh body, CreatureVisuals visuals)
    {
        body.Arms.SetBlendShapeWeight(ArmsBlendShapes.CylinderIndex, visuals.armsBlendShapes.Cylinder);
        body.Arms.SetBlendShapeWeight(ArmsBlendShapes.TaperIndex, visuals.armsBlendShapes.Taper);
    }

    private static void ApplyTorsoBlendShapeWeights(BodyMesh body, CreatureVisuals visuals)
    {
        body.Torso.SetBlendShapeWeight(TorsoBlendShapes.CylinderIndex, visuals.torsoBlendShapes.Cylinder);
        body.Torso.SetBlendShapeWeight(TorsoBlendShapes.TaperIndex, visuals.torsoBlendShapes.Taper);
    }

    private static void ApplyEyesBlendShapeWeights(BodyMesh body, CreatureVisuals visuals)
    {
        body.Eyes.SetBlendShapeWeight(EyesBlendShapes.SizeIndex, visuals.eyesBlendShapes.Size);
        body.Eyes.SetBlendShapeWeight(EyesBlendShapes.DistanceIndex, visuals.eyesBlendShapes.Distance);
        body.Eyes.SetBlendShapeWeight(EyesBlendShapes.CylinderIndex, visuals.eyesBlendShapes.Cylinder);
        body.Eyes.SetBlendShapeWeight(EyesBlendShapes.BoreIndex, visuals.eyesBlendShapes.Bore);
    }

    public static void ApplyHeadBlendShapeWeights(BodyMesh body, CreatureVisuals visuals)
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
    public struct MaterialSettings
    {
        public MaterialOption type;
        public FurSettings furSettings;
        public BSDFSettings bsdfSettings;
    }

    [System.Serializable]
    public enum MaterialOption
    {
        Fur,
        OpaqueBSDF,
        TransparentBSDF
    }

    [System.Serializable]
    public struct FurSettings
    {
        [Range(0, 1)]
        public float Metallic;

        [Range(0, 4)]
        public float Pattern;
        

        [Range(0, 1)]
        public float Smoothness;

        [Range(0.002f, 0.014f)]
        public float FurLength;

        [Range(0.6f, 12f)]
        public float FurDensity;

        [Range(0.15f, 0.005f)]
        public float FurThickness;
    }

    [System.Serializable]
    public struct BSDFSettings
    {
        [Range(0, 1)]
        public float Metallic;

        [Range(0, 1)]
        public float Smoothness;

        [Range(-0.5f, 1)]
        public float Emission;

        [Range(0, 1)]
        public float Opaqueness;
    }

    [System.Serializable]
    public struct HeadBlendShapes
    {
        [Range(-100f, 100f)]
        public float Float;

        [Range(-75f, 150f)]
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