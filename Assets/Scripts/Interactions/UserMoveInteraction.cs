using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class UserMoveParams : AInteractionParams
{
    public Vector3 position;
}

public class UserMoveRequest : AInteractionRequest<UserMoveInteraction, UserMoveParams>
{
    public UserMoveRequest(UserMoveParams parameters, CreatureIdentity target)
        : base(parameters, target,
            // Request Factory
            (p, participants) => new UserMoveInteraction(p, participants))
    { }
}

public class UserMoveInteraction : ACreatureInteraction<UserMoveParams>
{
    public UserMoveInteraction(UserMoveParams parameters, List<CreatureData> participants) : base(parameters, participants)
    { }

    public override string Name => "UserMove";

    public override string Description => "Initiator will move creature to target position";

    public override int MinParticipants => 1;

    public override int MaxParticipants => 10;

    public override bool AllowLateJoining => false;

    public override bool AllowEarlyLeaving => true;

    public override InteractionPriority Priority => InteractionPriority.User;

    public override bool InterruptLowerPriorityInteractions => true;

    protected override bool CheckLeave(CreatureData participantData)
    {
        bool result = AllParticipantsPastAction(0);

        // Debug.Log(
        //     $"{participantData.Identity} leave check = {result} " +
        //     $"index={StateOf(participantData).ActionIndex} " +
        //     $"complete={StateOf(participantData).ActionComplete}");

        return result;
    }

    protected override void UpdateInteraction(CreatureData p)
    {
        switch (StateOf(p).ActionIndex)
        {
            case 0:
                DispatchAction(p, DriverActions.MoveTo(Parameters.position));
                break;
        }
    }
    
    protected override void LeaveInteraction(CreatureData p)
    {
        DispatchAction(p, DriverActions.StopMoving());
    }
}