using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using UnityEngine;

public class ConversationParams : AInteractionParams
{
    public Vector3? position;
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
        if (!parameters.position)
        {
            var init = participants.FirstOrDefault(p => p.Identity != parameters.initiator);
            if (participants.Count == 2 && init != null)
            {
                gatherPosition = init.Driver.GetPosition();
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
        else
        {
            gatherPosition = parameters.position;
        }
    }

    public override string InteractionName => "Conversation";

    public override string Description => "Initiator will go talk to the other participant (if just 2, otherwise they will gather at the midpoint)";

    public override int MinParticipants => 2;

    public override int MaxParticipants => 20;

    public override bool AllowLateJoining => true;

    public override bool AllowEarlyLeaving => true;

    public override InteractionPriority Priority => 0;

    public override bool InterruptLowerPriorityInteractions => false;

    protected override bool CheckLeave(CreatureData participantData)
    {
        bool result = AllParticipantsPastAction(3);

        // Debug.Log(
        //     $"{participantData.Identity} leave check = {result} " +
        //     $"index={StateOf(participantData).ActionIndex} " +
        //     $"complete={StateOf(participantData).ActionComplete}");

        return result;
    }

    protected virtual bool IsSynchronizedAction(int actionIndex) { return actionIndex == 1; }

    protected override void UpdateInteraction(CreatureData p)
    {
        float rad;
        Vector3 participantSlot;
        switch (StateOf(p).ActionIndex)
        {
            case 0:
                rad = Mathf.Lerp(0, 2 * Mathf.PI, (float)IndexOf(p) / Participants.Count);
                participantSlot = gatherPosition + new Vector3((float)Mathf.Cos(rad), 0, (float)Mathf.Sin(rad)) * 3;
                Debug.Log($"Moving to participantSlot {participantSlot}");
                DispatchAction(p, DriverActions.MoveTo(participantSlot));
                break;

            case 1:
                // sync point right here... 
                // i.e actionIndex == 1 is dispatched synchronously/in the same interaction Tick
                // see IsSynchronizedAction(..) override and usage
                Debug.Log($"Creature[{p.Identity}] reached CHECKPOINT ActionIndex == 1.");
                DispatchAction(p, DriverActions.FacePosition(gatherPosition),
                    AllOf(
                        After(2f),
                        () => p.Driver.IsFacingPosition(gatherPosition)));
                break;

            case 2:
                Debug.Log($"Creature[{p.Identity}] reached CHECKPOINT ActionIndex == 2.");
                StateOf(p).ActionIndex++;
                break;
            case 3:
                DispatchAction(p, DriverActions.SetBool("IsDancing", true), After(10f));
                break;

            case 4:
                DispatchAction(p, DriverActions.Animator(d =>
                {
                    d.SetBool("IsDancing", false);
                    d.SetTrigger("Trip");
                }));
                break;

            case 5:
                rad = Mathf.Lerp(0, 2 * Mathf.PI, (float)IndexOf(p) / Participants.Count);
                participantSlot = gatherPosition - new Vector3((float)Mathf.Cos(rad), 0, (float)Mathf.Sin(rad)) * 2;
                Debug.Log($"Moving to participantSlot {participantSlot}");
                DispatchAction(p, DriverActions.MoveTo(participantSlot));
                break;
        }
    }
}