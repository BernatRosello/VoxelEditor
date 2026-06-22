using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public struct BodyMesh
{
    public SkinnedMeshRenderer Head;
    public SkinnedMeshRenderer Eyes;
    public SkinnedMeshRenderer Torso;
    public SkinnedMeshRenderer Arms;
    public SkinnedMeshRenderer Legs;
}

public class Creature : MonoBehaviour
{

    [SerializeField] private CreatureIdentity identity;
    [SerializeField] private CreatureVisuals visuals;
    [SerializeField] private BodyMesh bodyMesh;
    [SerializeField] private List<BodyPartCollider> bodyPartColliders;

    public CreatureIdentity Identity =>
        identity;

    public CreatureVisuals Visuals { get => visuals; set => visuals = value; }
    public BodyMesh BodyMesh { get => bodyMesh; set => bodyMesh = value; }

    [ContextMenu("Reassign GUID")]
    private void ResetGUID()
    {
        identity = CreatureIdentity.Create();
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        if (!identity.IsValid)
        {
            identity = CreatureIdentity.Create();

            UnityEditor.EditorUtility.SetDirty(this);
        }

        bodyMesh.Head = transform.Find("Head").GetComponent<SkinnedMeshRenderer>();
        bodyMesh.Eyes = transform.Find("Eyes").GetComponent<SkinnedMeshRenderer>();
        bodyMesh.Torso = transform.Find("Torso").GetComponent<SkinnedMeshRenderer>();
        bodyMesh.Arms = transform.Find("Arms").GetComponent<SkinnedMeshRenderer>();
        bodyMesh.Legs = transform.Find("Legs").GetComponent<SkinnedMeshRenderer>();

        bodyPartColliders = transform.GetComponentsInChildren<BodyPartCollider>().ToList();
    }

#endif

    [ContextMenu("Load Visuals")]
    private void LoadVisuals()
    {
        // Visual initialization
        if (visuals == null)
        {
            Debug.LogError($"Creature [{Identity}] has no visual data associated! Failed to init visuals, loading defaults...");
            CreatureVisuals newVisuals = Instantiate(CreatureEditorManager.Instance.DefaultVisuals);
            visuals = newVisuals;
            newVisuals.hideFlags = HideFlags.DontSave;
            // return;
        }
        CreatureVisuals.InitMaterialPropertyBlock();

        CreatureVisuals.ApplyMaterials(bodyMesh, visuals);
        CreatureVisuals.ApplyBlendShapeWeights(bodyMesh, visuals);
        CreatureVisuals.ApplyColors(bodyMesh, visuals);

    }

    public void EnableBodyPartColliders()
    {
        foreach(var col in bodyPartColliders)
        {
            col.EnableCollider();
        }
    }
    public void DisableBodyPartColliders()
    {
        foreach(var col in bodyPartColliders)
        {
            col.DisableCollider();
        }
    }

    private void Awake()
    {
        if (visuals == null)
        {
            Debug.LogWarning($"Creature [{Identity}] has no visual data associated, creating new default instance...");
            visuals = ScriptableObject.CreateInstance<CreatureVisuals>();
        }
        LoadVisuals();

    }
}