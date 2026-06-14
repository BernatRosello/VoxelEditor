using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConversationParams : AInteractionParams
{
    public float duration;
    public CreatureIdentity initiator;
}

public class ConversationRequest : AInteractionRequest<ConversationInteraction, ConversationParams>
{
    public ConversationRequest(ConversationParams parameters, IEnumerable<CreatureIdentity> targets)
        : base(parameters, targets,
            // Request Factory
            (p, participants) => new ConversationInteraction(p, participants))
    { }
}

public class ConversationInteraction : ACreatureInteraction<ConversationParams>
{
    Vector3 gatherPosition;
    public ConversationInteraction(ConversationParams parameters, List<CreatureData> participants) : base(parameters, participants)
    {
        if (participants.Count == 2)
        {
            gatherPosition = participants.First(p => p.Identity != parameters.initiator).Driver.GetPosition();
        }
        else
        {
            gatherPosition = Vector3.zero;
            float avgFac = 1.0f / participants.Count();
            foreach (var p in participants)
            {
                gatherPosition += p.Driver.GetPosition() * avgFac;
            }
        }
    }

    public override string InteractionName => "Conversation";

    public override string Description => "Initiator will go talk to the other participant (if just 2, otherwise they will gather at the midpoint)";

    public override int MinParticipants => 2;

    public override int MaxParticipants => 5;

    public override bool AllowLateJoining => true;

    public override bool AllowEarlyLeaving => true;

    public override InteractionPriority Priority => 0;

    public override bool InterruptLowerPriorityInteractions => false;

    protected override bool CheckLeave(CreatureData participantData)
    {
        bool result = AllParticipantsPastAction(3);

        Debug.Log(
            $"{participantData.Identity} leave check = {result} " +
            $"index={StateOf(participantData).ActionIndex} " +
            $"complete={StateOf(participantData).ActionComplete}");

        return result;
    }

    protected override void UpdateInteraction(CreatureData participant)
    {
        float rad;
        Vector3 participantSlot;
        switch (StateOf(participant).ActionIndex)
        {
            case 0:
                rad = Mathf.Lerp(0,2*Mathf.PI, (float)IndexOf(participant)/Participants.Count);
                participantSlot = gatherPosition + new Vector3((float)Mathf.Cos(rad),0, (float)Mathf.Sin(rad)) * 3;
                Debug.Log($"Moving to participantSlot {participantSlot}");
                DispatchAction(participant, DriverActions.MoveTo(participantSlot));
                break;

            case 1:
                DispatchAction(participant, DriverActions.SetTrigger("Trip"));
                break;

            case 2:
                rad = Mathf.Lerp(0,2*Mathf.PI, (float)IndexOf(participant)/Participants.Count);
                participantSlot = gatherPosition - new Vector3((float)Mathf.Cos(rad),0, (float)Mathf.Sin(rad)) * 2;
                Debug.Log($"Moving to participantSlot {participantSlot}");
                DispatchAction(participant, DriverActions.MoveTo(participantSlot));
                break;

            case 3:
                DispatchAction(participant, DriverActions.SetBool("IsDancing", true));
                break;
        }
    }
}