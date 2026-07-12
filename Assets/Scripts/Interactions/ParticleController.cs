using UnityEngine;
using System;
using System.Collections.Generic;



#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

[RequireComponent(typeof(ParticleSystem))]
public class ParticleController : MonoBehaviour
{
    [SerializeField] private new ParticleSystem particleSystem;
    private readonly List<Vector4> customData = new();

    void Awake()
    {
        if (particleSystem == null)
        {
            particleSystem = GetComponent<ParticleSystem>();
        }


        var customData = particleSystem.customData;
        customData.enabled = false;
        particleSystem.AllocateCustomDataAttribute(ParticleSystemCustomData.Custom1);
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        if (particleSystem == null)
            particleSystem = GetComponent<ParticleSystem>();
        EditorUtility.SetDirty(this);
    }
#endif

    public void EmitParticles(CreatureParticle particle, float scale = 1f, int count = 1)
{
    var emit = new ParticleSystem.EmitParams
    {
        startSize = scale,
    };

    particleSystem.Emit(emit, count);

    particleSystem.GetCustomParticleData(customData, ParticleSystemCustomData.Custom1);

    Debug.Log($"CustomData.Count = {customData.Count}");

    int start = Mathf.Max(0, customData.Count - count);

    for (int i = start; i < customData.Count; i++)
    {
        Vector4 v = customData[i];
        v.x = (float)particle;
        customData[i] = v;

        Debug.Log($"Setting particle {i} to {(int)particle}");
    }

    particleSystem.SetCustomParticleData(customData, ParticleSystemCustomData.Custom1);
}
}