using UnityEngine;
using Unity.Rendering;

[CreateAssetMenu(menuName = "Creature/Material Library")]
public class MaterialLibrary : ScriptableObject
{
    [SerializeField] private Material furMaterial;
    [SerializeField] private Material transparentBSDF;
    [SerializeField] private Material opaqueBSDF;

    [Header("Particle Materials")]
    [SerializeField] private Material conversation;
    [SerializeField] private Material pathing;
    [SerializeField] private Material pathingBlocked;
    [SerializeField] private Material energyHigh;
    [SerializeField] private Material energyLow;
    [SerializeField] private Material love;
    [SerializeField] private Material happy;
    [SerializeField] private Material sad;
    [SerializeField] private Material angry;
    [SerializeField] private Material cancel;
    [SerializeField] private Material bro;
    [SerializeField] private Material pathingMultiple;
    [SerializeField] private Material pointHand;
    [SerializeField] private Material puppet;
    [SerializeField] private Material questionMark;
    [SerializeField] private Material thumbsUp;
    [SerializeField] private Material thumbsDown;
    [SerializeField] private Material unkownPerson;
    [SerializeField] private Material knownPerson;
    [SerializeField] private Material musicNotes;
    [SerializeField] private Material babyNew;
    [SerializeField] private Material alien;
    [SerializeField] private Material investigate;
    [SerializeField] private Material handshake;
    [SerializeField] private Material partyHat;
    [SerializeField] private Material joke;
    [SerializeField] private Material receivingHands;


    public Material FurMaterial { get => furMaterial; }
    public Material OpaqueBSDF { get => opaqueBSDF; }
    public Material TransparentBSDF { get => transparentBSDF; }

    // Particles
    public Material ConversationParticle { get => conversation; set => conversation = value; }
    public Material PathingParticle { get => pathing; set => pathing = value; }
    public Material PathingBlockedParticle { get => pathingBlocked; set => pathingBlocked = value; }
    public Material LoveParticle { get => love; set => love = value; }
    public Material EnergyHighParticle { get => energyHigh; set => energyHigh = value; }
    public Material EnergyLowParticle { get => energyLow; set => energyLow = value; }
    public Material HappyParticle { get => happy; set => happy = value; }
    public Material SadParticle { get => sad; set => sad = value; }
    public Material AngryParticle { get => angry; set => angry = value; }
    public Material CancelParticle { get => cancel; set => cancel = value; }
    public Material BroParticle { get => bro; set => bro = value; }
    public Material PathingMultipleParticle { get => pathingMultiple; set => pathingMultiple = value; }
    public Material PointHandParticle { get => pointHand; set => pointHand = value; }
    public Material PuppetParticle { get => puppet; set => puppet = value; }
    public Material QuestionMarkParticle { get => questionMark; set => questionMark = value; }
    public Material ThumbsUpParticle { get => thumbsUp; set => thumbsUp = value; }
    public Material ThumbsDownParticle { get => thumbsDown; set => thumbsDown = value; }
    public Material UnkownPersonParticle { get => unkownPerson; set => unkownPerson = value; }
    public Material KnownPersonParticle { get => knownPerson; set => knownPerson = value; }
    public Material MusicNotesParticle { get => musicNotes; set => musicNotes = value; }
    public Material BabyNewParticle { get => babyNew; set => babyNew = value; }
    public Material AlienParticle { get => alien; set => alien = value; }
    public Material InvestigateParticle { get => investigate; set => investigate = value; }
    public Material HandshakeParticle { get => handshake; set => handshake = value; }
    public Material PartyHatParticle { get => partyHat; set => partyHat = value; }
    public Material JokeParticle { get => joke; set => joke = value; }
    public Material ReceivingHandsParticle { get => receivingHands; set => receivingHands = value; }
}