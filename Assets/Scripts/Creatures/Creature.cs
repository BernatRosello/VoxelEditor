using UnityEngine;

public class Creature : MonoBehaviour
{
    [SerializeField]
    private CreatureIdentity identity;

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

            UnityEditor.EditorUtility
                .SetDirty(this);
        }
    }

#endif
}