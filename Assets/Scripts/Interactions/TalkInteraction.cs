
using System;
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
    public override string DebugInfo => $"duration({TotalEllapsedTime}s/{Parameters.duration}s)";

    bool trigger = true;
    private const int avgEmotesPerMinute = 30;
    private Dictionary<CreatureIdentity, int> leaveActionIndex = new();

    protected override bool CheckLeave(CreatureData participantData)
    {
        return base.CheckLeave(participantData) || (TotalEllapsedTime >= Parameters.duration);
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

    protected override void AddParticipantData(CreatureData p)
    {
        leaveActionIndex[p.Identity] = 0;
    }

    protected override void JoinInteraction(CreatureData participant)
    {
        switch (StateOf(participant).ActionIndex)
        {
            case 0:
                NavMeshUtility.TryGetRandomPosition(GetConversationCenter(), 5, 1, out Vector3 talkingPos);

                // 20 second timeout as a safeguard to avoid stalling the interaction join
                DispatchAction(participant, DriverActions.EmitParticle(CreatureParticle.Pathing));      // Action 0
                DispatchAction(participant, DriverActions.MoveTo(talkingPos, participant.Stats.GetSpeed()), 20);                // Action 1
                break;
            case 2:
                DispatchAction(participant, DriverActions.FacePosition(GetConversationCenter())); // Action 2
                break;
            case 3:
                DispatchAction(participant, DriverActions.EmitParticle(CreatureParticle.Handshake));    // Action 3
                DispatchAction(participant, DriverActions.SetTrigger("GreetTrigger", "Greet Emote"));   // Action 4
                break;
        }
    }

    protected override bool JoinFinished(CreatureData participant)
    {
        return ParticipantFinishedAction(participant, 4);
    }

    protected override void UpdateInteraction(CreatureData participant, float deltaTime)
    {
        if (trigger)
        {
            DispatchAction(participant, DriverActions.EmitParticle(CreatureParticle.Conversation));         // Action 4 + n
            DispatchAction(participant, DriverActions.SetTrigger("TalkTrigger", waitOnState: "Talk Emote")); // Action 4 + n + 1
            StateOf(participant).ActionIndex = 1;
        }
        else
        {
            switch (StateOf(participant).ActionIndex)
            {
                case 1:
                    DispatchAction(participant, DriverActions.EmitParticle(participant.Stats.GetEmotionParticle()));
                    DispatchAction(participant, DriverActions.FacePosition(GetConversationCenter()), 3);
                    StateOf(participant).ActionIndex = 2;
                    break;
                default:
                    if (rnd.value < (avgEmotesPerMinute / 60f * deltaTime))
                    {
                        var mod = rnd.Range(-0.1f, 0.1f);
                        participant.Stats.Happiness += mod;
                        List<CreatureParticle> particles = new() { CreatureParticle.Happy };
                        if (mod > 0)
                            particles.Add(CreatureParticle.UpArrow);
                        else
                            particles.Add(CreatureParticle.DownArrow);
                        DispatchAction(participant, DriverActions.EmitParticles(particles));
                    }
                    break;
            }
        }
        continueUpdateTick = false;
        trigger = false;
        leaveActionIndex[participant.Identity] = StateOf(participant).ActionIndex + 1;
    }

    protected override void LeaveInteraction(CreatureData participant)
    {
        if (StateOf(participant).ActionIndex == leaveActionIndex[participant.Identity])
        {
            DispatchAction(participant, DriverActions.EmitParticle(CreatureParticle.Cancel));   // ActionIndex: leaveActionIndex[participant.Identity]
            Vector3 conversationCenter = new();
            foreach (var p in ActiveParticipants)
            {
                conversationCenter += 1f / ActiveParticipants.Count * p.Driver.GetPosition();
            }
            var leavePos = (conversationCenter - participant.Driver.GetPosition()).normalized * 3;

            // 10 second timeout as a safeguard to avoid stalling the interaction leave
            DispatchAction(participant, DriverActions.MoveTo(leavePos, participant.Stats.GetSpeed()), 5);               // ActionIndex: leaveActionIndex[participant.Identity] + 1
        }
    }

    protected override bool LeaveFinished(CreatureData participant)
    {
        return true;//ParticipantFinishedAction(participant, leaveActionIndex[participant.Identity]);
    }

    protected override void RemoveParticipantData(CreatureData p)
    {
        leaveActionIndex.Remove(p.Identity);
    }
}