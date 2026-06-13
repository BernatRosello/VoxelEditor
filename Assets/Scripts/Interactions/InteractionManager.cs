using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    private readonly Dictionary<CreatureIdentity, CreatureData> creatureData = new();
    private readonly Dictionary<CreatureData, ACreatureInteraction> participants = new();

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

        Creature[] identities = FindObjectsByType<Creature>();

        foreach (var c in identities)
        {
            ActionDriver driver =
                c.GetComponent<ActionDriver>();

            if (driver == null)
            {
                Debug.LogWarning(
                    $"Creature '{c.Identity.Guid}' has no ActionDriver.");
                continue;
            }

            RegisterParticipant(c.Identity, driver);
        }
    }

    private void Update()
    {
        ProcessRequests();

        for (int i = interactions.Count - 1; i >= 0; i--)
        {
            ACreatureInteraction interaction = interactions[i];

            interaction.Tick();

            if (interaction.IsInteractionEmpty())
            {
                interactions.RemoveAt(i);
            }
        }
    }

    #region Participants

    public CreatureData RegisterParticipant(CreatureIdentity identity, ActionDriver driver)
    {
        CreatureData participant = new(driver, identity);
        creatureData[identity] = participant;
        return participant;
    }

    public void UnregisterParticipant(CreatureData participant)
    {
        UnregisterParticipant(participant.Identity);
    }

    public void UnregisterParticipant(CreatureIdentity identity)
    {
        if (creatureData.TryGetValue(identity, out var p) && participants.ContainsKey(p))
        {
            participants[p].ForceLeave(p);
        }

        creatureData.Remove(identity);
    }

    private IEnumerable<CreatureData> ResolveParticipants(IEnumerable<CreatureIdentity> identities)
    {
        return identities.Select(x => creatureData[x]);
    }

    #endregion

    #region Requests

    private void ProcessRequests()
    {
        int requestsToProcess = requestQueue.Count;

        for (int i = 0; i < requestsToProcess; i++)
        {
            AInteractionRequest request = requestQueue.Dequeue();

            if (TryCreateInteraction(request))
            {
                continue;
            }

            request.RequestAttemptsLeft--;

            if (request.RequestAttemptsLeft > 0)
            {
                requestQueue.Enqueue(request);
            }
        }
    }

    #endregion

    #region Creation

    private bool TryCreateInteraction(AInteractionRequest request)
    {
        var potentialParticipants = ResolveParticipants(request.Targets);
        List<CreatureData> availableParticipants = new();

        ACreatureInteraction tempInter = request.CreateInteraction(potentialParticipants);
        if (!tempInter.ValidateInteraction())
        {
            return false;
        }

        foreach (CreatureData p in potentialParticipants)
        {
            ACreatureInteraction currentInteraction;
            if (participants.TryGetValue(p, out currentInteraction) &&
                tempInter.InterruptLowerPriorityInteractions &&
                currentInteraction.Priority < tempInter.Priority)
            {
                if (!TryLeaveInteraction(p))
                {
                    continue;
                }
            }
            availableParticipants.Add(p);
        }

        // Parameters are passed into the creation internally by the request holding them
        var newInteraction = request.CreateInteraction(availableParticipants);
        if (newInteraction.ValidateInteraction())
        {
            interactions.Add(newInteraction);
            foreach (var p in availableParticipants)
            {
                participants[p] = newInteraction;
            }
            return true;
        }
        return false;
    }

    #endregion

    #region Join

    public bool TryJoinInteraction(ACreatureInteraction inter, CreatureData p)
    {
        if (!interactions.Contains(inter) || !creatureData.ContainsKey(p.Identity))
        {
            return false;
        }
        if (!participants.TryGetValue(p, out var curr))
        {
            return inter.TryJoin(p);
        }
        else
        {
            return inter.InterruptLowerPriorityInteractions &&
                    curr.Priority < inter.Priority &&
                    curr.TryLeave(p) &&
                    inter.TryJoin(p);
        }
    }

    #endregion

    #region Leave

    public bool TryLeaveInteraction(CreatureData p)
    {
        if (!creatureData.ContainsKey(p.Identity) || !participants.TryGetValue(p, out ACreatureInteraction interaction))
        {
            return true;
        }

        return interaction.TryLeave(p);
    }

    public static void NotifyParticipantLeft(ACreatureInteraction i, CreatureData p)
    {
        if (!Instance)
            return;

        if (Instance.participants.TryGetValue(p, out var localInter) && localInter == i)
        {
            Instance.participants.Remove(p);
        }
    }

    #endregion

    #region Abort

    public void AbortInteraction(ACreatureInteraction interaction)
    {
        for (int i = interaction.Participants.Count - 1; i >= 0; i--)
        {
            CreatureData p = interaction.Participants[i];
            interaction.ForceLeave(p);
        }
    }

    #endregion

    #region Destruction


    #endregion
}
