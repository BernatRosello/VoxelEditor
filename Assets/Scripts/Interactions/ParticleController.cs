using UnityEngine;

public enum CreatureParticle
{
    Conversation,
    Pathing,
    PathingBlocked,
    Happy,
    Sad,
    Angry,
    EnergyHigh,
    EnergyLow,
    Love,
    Cancel,
    Bro,
    PathingMultiple,
    PointHand,
    Puppet,
    QuestionMark,
    ThumbsUp,
    ThumbsDown,
    UnkownPerson,
    KnownPerson,
    MusicNotes,
    BabyNew,
    Alien,
    Investigate,
    Handshake,
    PartyHat,
    Joke,
    ReceivingHands
}

[RequireComponent(typeof(ParticleSystem))]
public class ParticleController : MonoBehaviour
{
    [SerializeField] private new ParticleSystem particleSystem;
    void Awake()
    {
        if (particleSystem == null)
        {
            particleSystem = GetComponent<ParticleSystem>();
        }
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (particleSystem == null)
        {
            particleSystem = GetComponent<ParticleSystem>();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    public void EmitParticles(CreatureParticle particle, int count = 1)
    {
        particleSystem.GetComponent<ParticleSystemRenderer>().sharedMaterial = MaterialLibrarySingleton.GetMaterial(particle);
        particleSystem.Emit(count);
    }
}