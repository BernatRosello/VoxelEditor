using System;
using UnityEngine;

[Serializable]
public struct CreatureIdentity : IEquatable<CreatureIdentity>
{

    [SerializeField] private string guid;

    [SerializeField] private string originGame;

    [SerializeField] private string originVersion;

    [SerializeField] private long creationTimestamp;



    public string Guid => guid;

    public bool IsValid => !string.IsNullOrWhiteSpace(guid);
    

    public CreatureIdentity(string guid) : this()
    {
        this.guid = guid;
    }

    public static CreatureIdentity Create()
    {
        return new CreatureIdentity(CreatureGuidUtility.CreateGuid());
    }

    public bool Equals(CreatureIdentity other)
    {
        return guid == other.guid;
    }

    public override bool Equals(object obj)
    {
        return obj is CreatureIdentity other
            && Equals(other);
    }

    public override int GetHashCode()
    {
        return guid?.GetHashCode() ?? 0;
    }
    public override string ToString()
    {
        return guid;
    }

    public static bool operator ==(CreatureIdentity a, CreatureIdentity b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(CreatureIdentity a, CreatureIdentity b)
    {
        return !a.Equals(b);
    }
}