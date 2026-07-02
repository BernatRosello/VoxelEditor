using System.Collections.Generic;
using UnityEngine;

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
    public override InteractionPriority Priority => 0;
    public override bool InterruptLowerPriorityInteractions => false;

    private float totalEllapsedTime;

    protected override void PostTick(float deltaTime)
    {
        totalEllapsedTime += deltaTime;
    }

    protected override bool CheckLeave(CreatureData participantData)
    {
        return totalEllapsedTime >= Parameters.duration;
    }

    protected override void UpdateInteraction(CreatureData p)
    {
        if (StateOf(p).ActionIndex % 2 == 0)
        {
            float currentMoveDuration = Parameters.frequency + Random.Range(-1f, 1f) * Parameters.frequencyVariance;
            var dir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            if (Random.Range(0f, 1f) <= Parameters.moveChance)
            {
                Vector3 nextPosition = p.Driver.GetPosition() + dir * Random.Range(Parameters.minDistance, Parameters.maxDistance);
                // Version A - Makes sure that the character takes AT LEAST as much 
                //          currentMoveDuration time before moving again.
                // DispatchAction(p, DriverActions.MoveTo(nextPosition),
                //     AllOf(
                //         () => (lastMoveTime - totalEllapsedTime) >= currentMoveDuration,
                //         () => p.Driver.HasReachedDestination()
                //     ));

                // Version B - Changes target position as soon as the target either 
                // reaches the target position, OR the currentMoveDuration runs out.
                DispatchAction(p, DriverActions.MoveTo(nextPosition, p.Stats.Energy * 1.5f), currentMoveDuration);
            }
            else
            {
                DispatchAction(p, DriverActions.FaceDirection(dir), currentMoveDuration);
            }
        }
        else
        {
            // After each move decrease the energy as it is "used up"
            p.Stats.Energy -= 0.1f;
            DispatchAction(p, DriverActions.SetFloat("CalmEnergetic", p.Stats.Energy));
        }
    }

    protected override void LeaveInteraction(CreatureData p)
    {
        DispatchAction(p, DriverActions.StopMoving());
    }
}