using UnityEngine;

public class MaterialLibrarySingleton : MonoBehaviour
{
    private static MaterialLibrarySingleton Instance;
    [SerializeField] private MaterialLibrary m_materialLibrary;

    private static MaterialLibrary MaterialLibrary { get => Instance.m_materialLibrary; set => Instance.m_materialLibrary = value; }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }
#endif

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }


    public static Material GetMaterial(CreatureVisuals.MaterialOption mat)
    {
        switch (mat)
        {
            case CreatureVisuals.MaterialOption.Fur:
                return MaterialLibrary.FurMaterial;
            case CreatureVisuals.MaterialOption.OpaqueBSDF:
                return MaterialLibrary.OpaqueBSDF;
            case CreatureVisuals.MaterialOption.TransparentBSDF:
                return MaterialLibrary.TransparentBSDF;
            default:
                return null;
        }
    }

    public static Material GetMaterial(CreatureParticle particle)
    {
        switch (particle)
        {
            case CreatureParticle.Conversation:
                return MaterialLibrary.ConversationParticle;
            case CreatureParticle.Pathing:
                return MaterialLibrary.PathingParticle;
            case CreatureParticle.PathingBlocked:
                return MaterialLibrary.PathingBlockedParticle;
            case CreatureParticle.Happy:
                return MaterialLibrary.HappyParticle;
            case CreatureParticle.Sad:
                return MaterialLibrary.SadParticle;
            case CreatureParticle.Angry:
                return MaterialLibrary.AngryParticle;
            case CreatureParticle.EnergyHigh:
                return MaterialLibrary.EnergyHighParticle;
            case CreatureParticle.EnergyLow:
                return MaterialLibrary.EnergyLowParticle;
            case CreatureParticle.Love:
                return MaterialLibrary.LoveParticle;
            case CreatureParticle.Cancel:
                return MaterialLibrary.CancelParticle;
            case CreatureParticle.Bro:
                return MaterialLibrary.BroParticle;
            case CreatureParticle.PathingMultiple:
                return MaterialLibrary.PathingMultipleParticle;
            case CreatureParticle.PointHand:
                return MaterialLibrary.PointHandParticle;
            case CreatureParticle.Puppet:
                return MaterialLibrary.PuppetParticle;
            case CreatureParticle.QuestionMark:
                return MaterialLibrary.QuestionMarkParticle;
            case CreatureParticle.ThumbsUp:
                return MaterialLibrary.ThumbsUpParticle;
            case CreatureParticle.ThumbsDown:
                return MaterialLibrary.ThumbsDownParticle;
            case CreatureParticle.UnkownPerson:
                return MaterialLibrary.UnkownPersonParticle;
            case CreatureParticle.KnownPerson:
                return MaterialLibrary.KnownPersonParticle;
            case CreatureParticle.MusicNotes:
                return MaterialLibrary.MusicNotesParticle;
            case CreatureParticle.BabyNew:
                return MaterialLibrary.BabyNewParticle;
            case CreatureParticle.Alien:
                return MaterialLibrary.AlienParticle;
            case CreatureParticle.Investigate:
                return MaterialLibrary.InvestigateParticle;
            case CreatureParticle.Handshake:
                return MaterialLibrary.HandshakeParticle;
            case CreatureParticle.PartyHat:
                return MaterialLibrary.PartyHatParticle;
            case CreatureParticle.Joke:
                return MaterialLibrary.JokeParticle;
            case CreatureParticle.ReceivingHands:
                return MaterialLibrary.ReceivingHandsParticle;
            case CreatureParticle.UpArrow:
                return MaterialLibrary.UpArrowParticle;
            case CreatureParticle.DownArrow:
                return MaterialLibrary.DownArrowParticle;
            default:
                return null;
        }
    }
}