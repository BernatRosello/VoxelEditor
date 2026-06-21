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

    public CreatureIdentity Identity =>
        identity;

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
            identity =
                CreatureIdentity.Create();

            UnityEditor.EditorUtility.SetDirty(this);
        }

        bodyMesh.Head = transform.Find("Head").GetComponent<SkinnedMeshRenderer>();
        bodyMesh.Eyes = transform.Find("Eyes").GetComponent<SkinnedMeshRenderer>();
        bodyMesh.Torso = transform.Find("Torso").GetComponent<SkinnedMeshRenderer>();
        bodyMesh.Arms = transform.Find("Arms").GetComponent<SkinnedMeshRenderer>();
        bodyMesh.Legs = transform.Find("Legs").GetComponent<SkinnedMeshRenderer>();
    }

#endif

    [ContextMenu("Load Visuals")]
    private void LoadVisuals()
    {
        // Visual initialization
        if (visuals == null)
        {
            Debug.LogError($"Creature [{Identity}] has no visual data associated! Failed to init visuals...");
            return;
        }
        CreatureVisuals.SetMaterials(bodyMesh, visuals);
        CreatureVisuals.SetBlendShapeWeights(bodyMesh, visuals);
        CreatureVisuals.SetColors(bodyMesh, visuals);

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