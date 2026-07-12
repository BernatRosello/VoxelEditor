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
        bool result = ParticipantFinishedAction(participantData, 5);

        return result;
    }

    protected override void UpdateInteraction(CreatureData p, float deltaTime)
    {
        switch (StateOf(p).ActionIndex)
        {
            case 0:
                DispatchAction(p, DriverActions.EmitParticle(CreatureParticle.Puppet));             // Action 0
                DispatchAction(p, DriverActions.MoveTo(Parameters.position));                       // Action 1
                break;
            case 2:
                DispatchAction(p, DriverActions.EmitParticle(CreatureParticle.QuestionMark));       // Action 2
                DispatchAction(p, DriverActions.FacePosition(Camera.main.transform.position), 2);   // Action 3
                break;
            case 4:
                DispatchAction(p, DriverActions.EmitParticle(CreatureParticle.Handshake));          // Action 4
                DispatchAction(p, DriverActions.SetTrigger("GreetTrigger", "Greet Emote"));         // Action 5
                break;
        }
    }
    
    protected override void LeaveInteraction(CreatureData p)
    {
        DispatchAction(p, DriverActions.StopMoving());
    }
}