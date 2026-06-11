using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    private readonly Dictionary<CreatureIdentity, ParticipantData> participants = new();

    private readonly List<ACreatureInteraction> interactions = new();

    private readonly Queue<AInteractionRequest> requestQueue = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        ProcessRequests();

        for (int i = interactions.Count - 1; i >= 0; i--)
        {
            ACreatureInteraction interaction = interactions[i];

            interaction.Tick();

            if (interaction.IsFinished)
            {
                DestroyInteraction(interaction);
                interactions.RemoveAt(i);
            }
        }
    }

    #region Participants

    public ParticipantData RegisterParticipant(CreatureIdentity identity, ActionDriver driver)
    {
        ParticipantData participant = new(driver, identity);
        participants[identity] = participant;
        return participant;
    }

    public void UnregisterParticipant(ParticipantData participant)
    {
        UnregisterParticipant(participant.Identity);
    }

    public void UnregisterParticipant(CreatureIdentity identity)
    {
        if (participants.ContainsKey(identity) && participants[identity].CurrentInteraction != null)
        {
            participants[identity].CurrentInteraction.LeaveInteraction(participants[identity]);
        }

        participants.Remove(identity);
    }

    private IEnumerable<ParticipantData> ResolveParticipants(IEnumerable<CreatureIdentity> identities)
    {
        return identities.Select(x => participants[x]);
    }

    #endregion

    #region Requests

    private void ProcessRequests()
    {
        while (requestQueue.Count > 0)
        {
            AInteractionRequest request = requestQueue.Dequeue();
            TryCreateInteraction(request);
        }
    }

    #endregion

    #region Creation

    private void TryCreateInteraction(AInteractionRequest request)
    {
        var potentialParticipants = ResolveParticipants(request.Targets);
        List<ParticipantData> availableParticipants = new();

        var preInter = request.GetInteraction();

        int availableCount = 0;
        foreach (ParticipantData p in potentialParticipants)
        {
            if (p.CurrentInteraction != null &&
            preInter.InterruptLowerPriorityInteractions &&
            p.CurrentInteraction.Priority < preInter.Priority)
            {
                if (!TryLeaveInteraction(p))
                {
                    continue;
                }
            }
            else
            {
                availableCount++;
                availableParticipants.Add(p);
            }
        }

        var newInteraction = request.CreateInteraction(availableParticipants);

        // Perhaps should allow and wait on a transitory state where we wait for characters to finish  doing their previous interactions' last action
        if (!newInteraction.CheckStart())
        {
            return;
        }

        foreach (ParticipantData p in availableParticipants)
        {
            p.CurrentInteraction = newInteraction;
            // participant.Phase = ParticipantPhase.Start;
            p.ActionIndex = 0;
            p.ActionComplete = false;
        }

        newInteraction.StartInteraction();

        interactions.Add(newInteraction);
    }

    #endregion

    #region Join

    public bool TryJoinInteraction(ACreatureInteraction interaction, ParticipantData participant)
    {
        if (!interaction.AllowJoining)
        {
            return false;
        }

        if (participant.CurrentInteraction != null &&
            interaction.InterruptLowerPriorityInteractions &&
            participant.CurrentInteraction.Priority > interaction.Priority)
        {
            return false;
        }

        if (!interaction.TryJoin(participant))
        {
            return false;
        }

        participant.CurrentInteraction = interaction;
        // participant.Phase = ParticipantPhase.Start;
        participant.ActionIndex = 0;
        participant.ActionComplete = false;

        interaction.JoinInteraction(participant);

        return true;
    }

    #endregion

    #region Leave

    public bool TryLeaveInteraction(ParticipantData participant)
    {
        ACreatureInteraction interaction = participant.CurrentInteraction;

        if (interaction == null)
        {
            return false;
        }

        if (!interaction.AllowLeaving)
        {
            return false;
        }

        if (!interaction.TryLeave(participant))
        {
            return false;
        }

        interaction.LeaveInteraction(participant);
        participant.CurrentInteraction = null;

        return true;
    }

    #endregion

    #region Abort

    public void AbortInteraction(ACreatureInteraction interaction)
    {
        for (int i = interaction.Participants.Count - 1; i >= 0; i--)
        {
            ParticipantData participant = interaction.Participants[i];
            interaction.AbortParticipant(participant);
            participant.CurrentInteraction = null;
        }

        interactions.Remove(interaction);
    }

    #endregion

    #region Destruction

    private void DestroyInteraction(ACreatureInteraction interaction)
    {
        // Interaction must've already orderly ended and not have any participants...
        foreach (ParticipantData participant in interaction.Participants)
        {
            participant.CurrentInteraction = null;
        }
    }

    #endregion
}
