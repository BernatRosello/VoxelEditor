using System;
using UnityEngine;


public enum BodyPart
{
    Head,
    Eyes,
    Torso,
    Arms,
    Legs
}

public class BodyPartCollider : MonoBehaviour
{
    [SerializeField] private BodyPart bodyPart;
    private Collider coll;
    public BodyPart BodyPart { get => bodyPart; }
    public Creature creature;

#if UNITY_EDITOR

    private void OnValidate()
    {
        Awake();
    }
#endif

    private void Awake()
    {
        if (coll == null)
        {
            coll = GetComponent<Collider>();
        }
    }

    internal void DisableCollider()
    {
        coll.enabled = false;
    }

    internal void EnableCollider()
    {
        coll.enabled = true;
    }

}
