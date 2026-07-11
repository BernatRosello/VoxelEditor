using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

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
    ReceivingHands,
    UpArrow,
    DownArrow
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
            particleSystem = GetComponent<ParticleSystem>();

        var tsa = particleSystem.textureSheetAnimation;

        while (tsa.spriteCount > 0)
            tsa.RemoveSprite(0);

        const string folder = "Assets/Sprites/CreatureParticles";

        var sprites = AssetDatabase.FindAssets("t:Sprite", new[] { folder })
            .Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g)))
            .OrderBy(s => s.name)
            .ToArray();

        foreach (var sprite in sprites)
            tsa.AddSprite(sprite);

        EditorUtility.SetDirty(particleSystem);
    }
#endif

// TODO: THIS APPROACH IS NOT POSSIBLE BECAUSE UNITY FUCKING SUCKS SO WE ARE GOING TO NEED TO HAVE A SINGLE FUCKING PARTICLE SYSTEM PER-PARTICLE SPRITE!
    public void EmitParticles(CreatureParticle particle, float scale = 1f, int count = 1)
    {
        float frameScalar = (float)frame/ (float)( textureSheetAnim.numTilesX * textureSheetAnim.numTilesY );
        textureSheetAnim.startFrame = new ParticleSystem.MinMaxCurve( frameScalar );
        particleSystem.textureSheetAnimation.startFrame = particle;
        var emit = new ParticleSystem.EmitParams
        {
            startSize = scale
        };

        particleSystem.Emit(emit, count);
    }
}