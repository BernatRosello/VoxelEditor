
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using rnd = UnityEngine.Random;

public class TalkParams : AInteractionParams
{
    public float duration;
}

public class TalkInteractionRequest : AInteractionRequest<TalkInteraction, TalkParams>
{
    public TalkInteractionRequest(TalkParams parameters, IEnumerable<CreatureIdentity> targets)
        : base(parameters, targets,
            // Request Factory
            (p, participants) => new TalkInteraction(p, participants))
    { }
}

public class TalkInteraction : ACreatureInteraction<TalkParams>
{
    public TalkInteraction(TalkParams parameters, List<CreatureData> participants) : base(parameters, participants)
    {
    }

    public override string Name => "Talk";

    public override string Description => "";

    public override int MinParticipants => 2;

    public override int MaxParticipants => 8;

    public override bool AllowLateJoining => true;

    public override bool AllowEarlyLeaving => true;

    public override InteractionPriority Priority => InteractionPriority.Normal;

    public override bool InterruptLowerPriorityInteractions => true;

    bool trigger = true;
    private const int avgEmotesPerMinute = 20;
    private Dictionary<CreatureIdentity, int> leaveActionIndex = new();

    protected override bool CheckLeave(CreatureData participantData)
    {
        return base.CheckLeave(participantData) && TotalEllapsedTime >= Parameters.duration;
    }

    protected override void PostTick(float deltaTime)
    {
        trigger = rnd.value < (avgEmotesPerMinute / 60f * deltaTime);
    }

    private Vector3 GetConversationCenter()
    {
        Vector3 center = new();
        foreach (var p in ActiveParticipants)
        {
            center += 1f / ActiveParticipants.Count * p.Driver.GetPosition();
        }
        return center;
    }

    protected override void JoinInteraction(CreatureData participant)
    {
        if (StateOf(participant).ActionIndex == 0)
        {
            NavMeshUtility.TryGetRandomPosition(GetConversationCenter(), 5, 1, out Vector3 talkingPos);

            // 20 second timeout as a safeguard to avoid stalling the interaction join
            DispatchAction(participant, DriverActions.MoveTo(talkingPos, 2.5f), 20);
        }
    }

    protected override bool JoinFinished(CreatureData participant)
    {
        return true;//ParticipantFinishedAction(participant, 0);
    }

    protected override void UpdateInteraction(CreatureData participant)
    {
        if (trigger)
        {
            DispatchAction(participant, DriverActions.EmitParticle(CreatureParticle.Conversation));
            Debug.Log($"[{participant.Identity}] Triggered Talking anim!");
            DispatchAction(participant, DriverActions.SetTrigger("TalkTrigger"));
        }
        else
        {
            DispatchAction(participant, DriverActions.FacePosition(GetConversationCenter()), 3);
        }
        continueUpdateTick = false;
        trigger = false;
        leaveActionIndex[participant.Identity] = StateOf(participant).ActionIndex + 1;
    }

    protected override void LeaveInteraction(CreatureData participant)
    {
        if (StateOf(participant).ActionIndex == leaveActionIndex[participant.Identity])
        {
            Vector3 conversationCenter = new();
            foreach (var p in ActiveParticipants)
            {
                conversationCenter += 1f / ActiveParticipants.Count * p.Driver.GetPosition();
            }
            var leavePos = (conversationCenter - participant.Driver.GetPosition()).normalized * 3;

            // 10 second timeout as a safeguard to avoid stalling the interaction leave
            DispatchAction(participant, DriverActions.MoveTo(leavePos, 2.5f), 10);
        }
    }

    protected override bool LeaveFinished(CreatureData participant)
    {
        return true;//ParticipantFinishedAction(participant, leaveActionIndex[participant.Identity]);
    }
}