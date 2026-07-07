using System.Collections.Generic;
using rnd = UnityEngine.Random;

public class IdleParams : AInteractionParams
{
    public float duration;
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

    float ellapsed;
    bool trigger;

    protected override bool CheckLeave(CreatureData participantData)
    {
        return base.CheckLeave(participantData) && ellapsed >= Parameters.duration;
    }

    protected override void PostTick(float deltaTime)
    {
        ellapsed += deltaTime;
        trigger = rnd.value < (1 / 60f * deltaTime);
    }

    protected override void UpdateInteraction(CreatureData participant)
    {
        if (trigger)
        {
            trigger = false;
            DispatchAction(participant, DriverActions.SetTrigger("EmoteTrigerr"));
        }
    }
}