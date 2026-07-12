using UnityEngine;
using Unity.Rendering;

[CreateAssetMenu(menuName = "Creature/Material Library")]
public class MaterialLibrary : ScriptableObject
{
    [SerializeField] private Material furMaterial;
    [SerializeField] private Material transparentBSDF;
    [SerializeField] private Material opaqueBSDF;


    public Material FurMaterial { get => furMaterial; }
    public Material OpaqueBSDF { get => opaqueBSDF; }
    public Material TransparentBSDF { get => transparentBSDF; }
}