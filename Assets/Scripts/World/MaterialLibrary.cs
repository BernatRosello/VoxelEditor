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
}