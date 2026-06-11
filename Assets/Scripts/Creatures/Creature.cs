using UnityEngine;

public class Creature : MonoBehaviour
{
    [SerializeField]
    private CreatureIdentity identity;

    public CreatureIdentity Identity =>
        identity;

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