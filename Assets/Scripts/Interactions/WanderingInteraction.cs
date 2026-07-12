using System.Collections.Generic;
using UnityEngine;
using rnd = UnityEngine.Random;

public class WanderingParams : AInteractionParams
{
    public float duration;
    public float minDistance;
    public float maxDistance;
    public float frequency;
    public float frequencyVariance;
    public float moveChance;
}

public class WanderingRequest : AInteractionRequest<WanderingInteraction, WanderingParams>
{
    public WanderingRequest(WanderingParams parameters, IEnumerable<CreatureIdentity> targets)
        : base(parameters, targets,
            // Request Factory
            (p, participants) => new WanderingInteraction(p, participants))
    { }
    public WanderingRequest(WanderingParams parameters, CreatureIdentity target)
        : base(parameters, target,
            // Request Factory
            (p, participants) => new WanderingInteraction(p, participants))
    { }
}

public class WanderingInteraction : ACreatureInteraction<WanderingParams>
{
    public WanderingInteraction(WanderingParams parameters, List<CreatureData> participants) : base(parameters, participants)
    { }

    public override string Name => "Wandering";
    public override string Description => "random movement";
    public override int MinParticipants => 1;
    public override int MaxParticipants => 1;
    public override bool AllowLateJoining => false;
    public override bool AllowEarlyLeaving => true;
    public override InteractionPriority Priority => InteractionPriority.Background;
    public override bool InterruptLowerPriorityInteractions => false;
    public override string DebugInfo =>
    $@"
    duration: {TotalEllapsedTime}s/{Parameters.duration}s
    minDistance: {Parameters.minDistance}
    maxDistance: {Parameters.maxDistance}
    frequency: {Parameters.frequency}
    frequencyVariance: {Parameters.frequencyVariance}
    moveChance: {Parameters.moveChance}";

    protected override bool CheckLeave(CreatureData participantData)
    {
        return TotalEllapsedTime >= Parameters.duration;
    }

    protected override void UpdateInteraction(CreatureData p, float deltaTime)
    {
        float currentMoveDuration = Parameters.frequency + Random.Range(-1f, 1f) * Parameters.frequencyVariance;
        var dir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        switch (StateOf(p).ActionIndex)
        {
            case 0:
                DispatchAction(p, DriverActions.EmitParticle(CreatureParticle.Pathing));
                Vector3 nextPosition = p.Driver.GetPosition() + dir * Random.Range(Parameters.minDistance, Parameters.maxDistance);
                DispatchAction(p, DriverActions.MoveTo(nextPosition, p.Stats.GetSpeed()), currentMoveDuration);
                break;
            case 1:
                DispatchAction(p, DriverActions.EmitParticle(CreatureParticle.PointHand));
                DispatchAction(p, DriverActions.FaceDirection(dir), currentMoveDuration);
                break;
            case 2:

                List<CreatureParticle> particles = new() { CreatureParticle.EnergyHigh, CreatureParticle.DownArrow };
                DispatchAction(p, DriverActions.EmitParticles(particles));
                p.Stats.Energy -= rnd.value * 0.1f;
                break;
            default:
                if (Random.Range(0f, 1f) <= Parameters.moveChance)
                    StateOf(p).ActionIndex = 0;
                    break;
        }
    }

    protected override void LeaveInteraction(CreatureData p)
    {
        DispatchAction(p, DriverActions.StopMoving());
    }
}