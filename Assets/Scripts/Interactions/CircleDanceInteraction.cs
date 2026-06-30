using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CircleDanceParams : AInteractionParams
{
    public Vector3? position;
    public float duration;
    public CreatureIdentity initiator;
}

public class CircleDanceRequest : AInteractionRequest<CircleDanceInteraction, CircleDanceParams>
{
    public CircleDanceRequest(CircleDanceParams parameters, IEnumerable<CreatureIdentity> targets)
        : base(parameters, targets,
            // Request Factory
            (p, participants) => new CircleDanceInteraction(p, participants))
    { }
}

public class CircleDanceInteraction : ACreatureInteraction<CircleDanceParams>
{
    Vector3 gatherPosition;
    public CircleDanceInteraction(CircleDanceParams parameters, List<CreatureData> participants) : base(parameters, participants)
    {
        if (parameters == null || !parameters.position.HasValue)
        {
            CreatureData init;
            if (parameters == null || parameters.initiator == null)
                init = participants[0];
            else
                init = participants.FirstOrDefault(p => p.Identity != parameters.initiator);

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
            gatherPosition = Parameters.position.Value;
        }
    }

    public override string Name => "CircleDance";
    public override string Description => "Initiator will go talk to the other participant (if just 2, otherwise they will gather at the midpoint)";
    public override int MinParticipants => 2;
    public override int MaxParticipants => 20;
    public override bool AllowLateJoining => true;
    public override bool AllowEarlyLeaving => true;
    public override InteractionPriority Priority => InteractionPriority.Normal;
    public override bool InterruptLowerPriorityInteractions => true;

    protected override bool CheckLeave(CreatureData participantData)
    {
        bool result = AllParticipantsPastAction(5);//&& base.CheckLeave(participantData);

        // Debug.Log(
        //     $"{participantData.Identity} leave check = {result} " +
        //     $"index={StateOf(participantData).ActionIndex} " +
        //     $"complete={StateOf(participantData).ActionComplete}");

        return result;
    }

    protected override bool IsSynchronizedAction(int actionIndex) { return actionIndex == 1; }

    protected override void UpdateInteraction(CreatureData p)
    {
        float rad;
        Vector3 participantSlot;
        switch (StateOf(p).ActionIndex)
        {
            case 0:
                rad = Mathf.Lerp(0, 2 * Mathf.PI, (float)IndexOf(p) / ActiveParticipants.Count);
                participantSlot = gatherPosition + new Vector3((float)Mathf.Cos(rad), 0, (float)Mathf.Sin(rad)) * 3;
                // Debug.Log($"Moving to participantSlot {participantSlot}");
                DispatchAction(p, DriverActions.MoveTo(participantSlot));
                break;

            case 1:
                // sync point right here... 
                // i.e actionIndex == 1 is dispatched synchronously/in the same interaction Tick
                // see IsSynchronizedAction(..) override and usage
                // Debug.Log($"Creature[{p.Identity}] reached CHECKPOINT ActionIndex == 1.");
                DispatchAction(p, DriverActions.FacePosition(gatherPosition), 2);//, //After(2f));
                    // AnyOf(
                    //     AllOf(After(2f), () => p.Driver.IsFacingPosition(gatherPosition)),
                    //     After(10f)));
                break;

            case 2:
                Debug.Log($"Creature[{p.Identity}] reached CHECKPOINT ActionIndex == 2.");
                StateOf(p).ActionIndex++;
                break;
            case 3:
                // Debug.Log($"Creature[{p.Identity}] reached CHECKPOINT ActionIndex == 3.");
                DispatchAction(p, DriverActions.SetBool("IsDancing", true), After(10f));
                break;

            case 4:
                // Debug.Log($"Creature[{p.Identity}] reached CHECKPOINT ActionIndex == 4.");
                DispatchAction(p, DriverActions.Animator(d =>
                {
                    d.SetBool("IsDancing", false);
                    // d.SetTrigger("Trip");
                }));
                break;

            case 5:
                // Debug.Log($"Creature[{p.Identity}] reached CHECKPOINT ActionIndex == 5.");
                rad = Mathf.Lerp(0, 2 * Mathf.PI, (float)IndexOf(p) / ActiveParticipants.Count);
                participantSlot = gatherPosition - new Vector3((float)Mathf.Cos(rad), 0, (float)Mathf.Sin(rad)) * 3;
                // Debug.Log($"Moving to participantSlot {participantSlot}");
                DispatchAction(p, DriverActions.MoveTo(participantSlot));
                break;

            default:
                Debug.Log($"C[{p.Identity}] ActionIndex out of scripted range: {StateOf(p).ActionIndex} ActionComplete({StateOf(p).ActionComplete})");
                break;
        }
    }

    protected override void LeaveInteraction(CreatureData participant)
    {
        DispatchAction(participant, DriverActions.ResetAnimator());
    }
}