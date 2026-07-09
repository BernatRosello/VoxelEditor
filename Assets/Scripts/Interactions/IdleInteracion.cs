using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using rnd = UnityEngine.Random;

public class IdleParams : AInteractionParams
{
    public float duration;
    public int avgEmotesPerMinute;
}

public class IdleInteractionRequest : AInteractionRequest<IdleInteraction, IdleParams>
{

    public IdleInteractionRequest(IdleParams parameters, CreatureIdentity target)
        : base(parameters, target,
            // Request Factory
            (p, participants) => new IdleInteraction(p, participants))
    { }
}

public class IdleInteraction : ACreatureInteraction<IdleParams>
{
    public IdleInteraction(IdleParams parameters, List<CreatureData> participants) : base(parameters, participants)
    {
    }

    public override string Name => "Idle";

    public override string Description => "";

    public override int MinParticipants => 1;

    public override int MaxParticipants => 1;

    public override bool AllowLateJoining => false;

    public override bool AllowEarlyLeaving => true;

    public override InteractionPriority Priority => InteractionPriority.Background;

    public override bool InterruptLowerPriorityInteractions => false;

    bool trigger = true;

    protected override bool CheckLeave(CreatureData participantData)
    {
        return base.CheckLeave(participantData) && TotalEllapsedTime >= Parameters.duration;
    }

    protected override void PostTick(float deltaTime)
    {
        trigger = rnd.value < (Parameters.avgEmotesPerMinute / 60f * deltaTime);
    }

    protected override void UpdateInteraction(CreatureData participant)
    {
        if (trigger)
        {
            DispatchAction(participant, DriverActions.EmitParticle(CreatureParticle.Cancel, 3));
            Debug.Log($"[{participant.Identity}] Triggered an emote");
            DispatchAction(participant, DriverActions.SetTrigger("EmoteTrigger"));
        }
        continueUpdateTick = false;
        trigger = false;
    }
}