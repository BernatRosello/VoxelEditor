using UnityEngine;
using Unity.Rendering;

[CreateAssetMenu(menuName = "Creature/Material Library")]
public class MaterialLibrary : ScriptableObject
{

    [SerializeField] private Material furMaterial;
    [SerializeField] private Material bsdf;

    public Material FurMaterial { get => furMaterial; }
    public Material BSDF { get => bsdf; }

    }