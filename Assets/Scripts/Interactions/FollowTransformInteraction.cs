using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class FollowTransformParams : AInteractionParams
{
    public Transform followTarget;
    public float duration = 40;
}

public class FollowTransformRequest : AInteractionRequest<FollowTransformInteraction, FollowTransformParams>
{
    public FollowTransformRequest(FollowTransformParams parameters, CreatureIdentity target)
        : base(parameters, target,
            // Request Factory
            (p, participants) => new FollowTransformInteraction(p, participants))
    { }
}

public class FollowTransformInteraction : ACreatureInteraction<FollowTransformParams>
{
    public FollowTransformInteraction(FollowTransformParams parameters, List<CreatureData> participants) : base(parameters, participants)
    { }

    public override string Name => "Follow Transform";

    public override string Description => "Initiator will move creature to target position";

    public override int MinParticipants => 1;

    public override int MaxParticipants => 1;

    public override bool AllowLateJoining => false;

    public override bool AllowEarlyLeaving => true;

    public override InteractionPriority Priority => InteractionPriority.Normal;

    public override bool InterruptLowerPriorityInteractions => true;
    
    private float totalEllapsedTime;

    protected override void PostTick(float deltaTime)
    {
        totalEllapsedTime += deltaTime;
    }

    protected override bool CheckLeave(CreatureData participantData)
    {
        return (totalEllapsedTime >= Parameters.duration) || Parameters.followTarget == null;
    }
       
    protected override void UpdateInteraction(CreatureData p)
    {
        Vector3 targetPos = Parameters.followTarget.position;
        Vector3 moveVec = p.Driver.GetPosition() - targetPos;
        Vector3 movePos = targetPos - moveVec.normalized * 0.5f;
        DispatchAction(p, DriverActions.MoveTo(movePos));
    }
    
    protected override void LeaveInteraction(CreatureData p)
    {
        DispatchAction(p, DriverActions.StopMoving());
    }
}