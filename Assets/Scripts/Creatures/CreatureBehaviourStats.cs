using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[Serializable]
public sealed class CreatureBehaviourStats
{
    [SerializeField] private float happiness;
    [SerializeField] private float energy;

    private Animator animator;

    public float Happiness
    {
        get => happiness;
        set => SetField(ref happiness, value);
    }

    public float Energy
    {
        get => energy;
        set => SetField(ref energy, value);
    }

    public void BindAnimator(Animator animator)
    {
        this.animator = animator;
        UpdateAnimator();
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetFloat("CalmEnergetic", Energy);
        animator.SetFloat("AngryHappy", Happiness);
    }

    public CreatureParticle GetEmotionParticle()
    {
        if (Happiness > 0.5f)
            return CreatureParticle.Happy;

        return Energy > 0.5f
            ? CreatureParticle.Angry
            : CreatureParticle.Sad;
    }

    public void SetStats(CreatureBehaviourStats other)
    {
        Happiness = other.Happiness;
        Energy = other.Energy;
    }
}