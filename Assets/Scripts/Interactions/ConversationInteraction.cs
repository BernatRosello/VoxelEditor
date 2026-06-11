using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class ConversationParams : AInteractionParams
{
    public float duration;
    public CreatureIdentity initiator;
}

public class ConversationInteraction : ACreatureInteraction<ConversationParams>
{
    Vector3 gatherPosition;
    public ConversationInteraction(ConversationParams parameters, List<ParticipantData> participants) : base(parameters, participants)
    {
        if (participants.Count == 2)
        {
            gatherPosition = participants.First(p => p.Identity == parameters.initiator).Driver.GetPosition();
        }
    }

    public override string InteractionName => "Conversation";

    public override string Description => "Initiator will go talk to the other participant (if just 2, otherwise they will gather at the midpoint)";

    public override int MinParticipants => 2;

    public override int MaxParticipants => 5;

    public override bool AllowJoining => true;

    public override bool AllowLeaving => true;

    public override InteractionPriority Priority => 0;

    public override bool InterruptLowerPriorityInteractions => false;

    public override bool IsFinished => Participants.Count < MinParticipants;

    public override bool CheckEnd()
    {
        // Something for done talking?? idk
        throw new System.NotImplementedException();
    }

    public override void UpdateInteraction()
    {
        // Beginning Phase
        // walk to gathering position

        // Talk Phase
        // talking animation for random amount of time

        // Leave Phase
        // 
    }
}