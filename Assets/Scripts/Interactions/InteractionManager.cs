using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public sealed class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    private readonly Dictionary<CreatureIdentity, CreatureData> creatureData = new();
    private readonly Dictionary<CreatureData, ACreatureInteraction> participants = new();
    private readonly Dictionary<CreatureData, ACreatureInteraction> waitingToJoin = new();

    public static IReadOnlyCollection<CreatureIdentity> Creatures => Instance.creatureData.Keys;

    private readonly List<ACreatureInteraction> interactions = new();

    private readonly Queue<AInteractionRequest> requestQueue = new();

    [SerializeField] private float TPS = 20;
    private float TickTime => 1.0f / TPS;

    public static IReadOnlyCollection<ACreatureInteraction> Interactions => Instance.interactions;

    private float tickTimer;

    [Header("NavMesh Configurations")]
    public float AvoidancePredictionTime = 2;
    public int PathfindingIterationsPerFrame = 100;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        tickTimer = 0;

        Creature[] creaturesInScene = FindObjectsByType<Creature>();

        foreach (var c in creaturesInScene)
        {
            ActionDriver driver =
                c.GetComponent<ActionDriver>();

            if (driver == null)
            {
                Debug.LogWarning(
                    $"Creature '{c.Identity.Guid}' has no ActionDriver.");
                continue;
            }

            RegisterParticipant(c.Identity, c.Stats, driver);
        }
    }

    [ContextMenu("Create CircleDance")]
    private void CreateCircleDance()
    {
        var newReq = new CircleDanceRequest(null, creatureData.Keys.AsEnumerable());
        CreateRequest(newReq);
    }

    private void Update()
    {
        UnityEngine.AI.NavMesh.avoidancePredictionTime = AvoidancePredictionTime;
        UnityEngine.AI.NavMesh.pathfindingIterationsPerFrame = PathfindingIterationsPerFrame;
        tickTimer += Time.deltaTime;

        if (tickTimer < TickTime)
        {
            return;
        }
        tickTimer = 0;

        ProcessLateJoins();
        ProcessRequests();

        for (int i = interactions.Count - 1; i >= 0; i--)
        {
            ACreatureInteraction interaction = interactions[i];

            interaction.Tick(TickTime);

            if (interaction.IsEmpty)
            {
                Debug.Log($"Removed '{interaction.Name}' Interaction in no longer valid state");
                interactions.RemoveAt(i);
            }
        }
    }

    private void ProcessLateJoins()
    {
        List<CreatureData> remove = new();
        foreach (var kvp in waitingToJoin)
        {
            if (!interactions.Contains(kvp.Value))
            {
                Debug.Log($"Interaction [{kvp.Value}] got resolved before [{kvp.Key.Identity}] could complete LateJoining.");
                remove.Add(kvp.Key);
            }
            else if (!participants.ContainsKey(kvp.Key)) // Check if the participant has left it's prev interaction yet (thats how he ended up in the waiting Map)
            {
                if (kvp.Value.TryJoin(kvp.Key))
                {
                    Debug.Log($"Creature [{kvp.Key.Identity}] succesfully LateJoined Interaction [{kvp.Value}]");
                    participants[kvp.Key] = kvp.Value;
                    remove.Add(kvp.Key);
                }
                else
                {
                    Debug.Log($"Creature [{kvp.Key.Identity}] can't LateJoin Interaction [{kvp.Value}] (failed CanJoin() check...) TODO: Consider evaluating the waitingToJoin status for discard if it fails to join consistently");
                }
            }
            else
            {
                Debug.Log($"Creature [{kvp.Key.Identity}] can't LateJoin Interaction [{kvp.Value}] yet, it's still occupied with the previous interaction's ({participants[kvp.Key].TryReadState(kvp.Key).Phase})!");
            }
        }
        foreach (var r in remove)
        {
            waitingToJoin.Remove(r);
        }
    }

    #region Participants
    public static CreatureData TryGetCreatureData(CreatureIdentity creature)
    {
        if (!Instance) return null;
        Instance.creatureData.TryGetValue(creature, out var data);
        return data;
    }
    public static ACreatureInteraction TryGetInteraction(CreatureData creature)
    {
        if (!Instance) return null;
        Instance.participants.TryGetValue(creature, out var interaction);
        return interaction;
    }

    public CreatureData RegisterParticipant(CreatureIdentity identity, CreatureBehaviourStats stats, ActionDriver driver)
    {
        CreatureData participant = new(driver, stats, identity);
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

    public static IEnumerable<CreatureData> ResolveParticipants(IEnumerable<CreatureIdentity> identities)
    {
        if (!Instance) return null;
        return identities.Select(x => Instance.creatureData[x]);
    }

    #endregion

    #region Requests

    public static void CreateRequest(AInteractionRequest req)
    {
        if (!Instance)
            return;

        Instance.requestQueue.Enqueue(req);
    }

    private void ProcessRequests()
    {
        int requestsToProcess = requestQueue.Count;

        for (int i = 0; i < requestsToProcess; i++)
        {
            AInteractionRequest request = requestQueue.Dequeue();
            if (request.Promised || TryPrepareRequest(request))
            {
                if (!TryFulfillRequest(request))
                {
                    // If it can't be fulfilled right now let it go around once more
                    // * Promised participants will be leaving, which will block them
                    //      from being promised to other requests.
                    requestQueue.Enqueue(request);
                }
            }
            else
            {
                request.RequestAttemptsLeft--;

                if (request.RequestAttemptsLeft > 0)
                {
                    requestQueue.Enqueue(request);
                }
            }
        }
    }

    #endregion

    #region Creation
    private bool TryFulfillRequest(AInteractionRequest request)
    {
        List<CreatureData> available = new();

        foreach (var pp in request.PromisedParticipants)
        {
            var p = creatureData[pp.Identity];

            // return out if any participant is still busy in another interaction
            if (participants.ContainsKey(p))
            {
                Debug.Log($"Tried to add promised participant[{p.Identity}] that is still busy with other action!");
                return false;
            }

            available.Add(p);
        }

        var interaction = request.CreateInteraction(available);

        if (!interaction.ValidateInteraction())
            return false;

        interactions.Add(interaction);

        foreach (var p in available)
        {
            participants[p] = interaction;
        }

        return true;
    }
    private bool TryPrepareRequest(AInteractionRequest request)
    {
        var potentialParticipants = ResolveParticipants(request.Targets);
        List<CreatureData> availableParticipants = new();

        ACreatureInteraction tempInter = request.CreateInteraction(potentialParticipants);
        if (!tempInter.ValidateInteraction())
        {
            Debug.Log($"Failed to Pre-Validate InteractionRequest for [{tempInter.Name}] ");
            return false;
        }

        foreach (CreatureData p in potentialParticipants)
        {
            ACreatureInteraction currentInteraction;
            if (!participants.TryGetValue(p, out currentInteraction))
            {
                availableParticipants.Add(p);
                continue;
            }
            if (tempInter.InterruptLowerPriorityInteractions &&
                currentInteraction.Priority <= tempInter.Priority && // <= allows same priority currentInteraction to be interrupted
                tempInter.CanJoin(p)) // Preemptive check to avoid participant "orphanage"
            {
                TryLeaveInteraction(p);
            }
        }

        // Parameters are passed into the creation internally by the request holding them
        var newInteraction = request.CreateInteraction(availableParticipants);
        if (newInteraction.ValidateInteraction())
        {
            request.Promised = true;
            request.PromisedParticipants.Clear();
            request.PromisedParticipants.AddRange(availableParticipants);
            return true;
        }
        return false;
    }

    #endregion

    #region Join

    private bool CanJoin(CreatureData participant, ACreatureInteraction joiningInteraction)
    {
        if (!creatureData.ContainsKey(participant.Identity))
        {
            return false;
        }
        else if (waitingToJoin.ContainsKey(participant))
        {
            return waitingToJoin[participant] == joiningInteraction;
        }
        if (!participants.TryGetValue(participant, out var currentInteraction))
        {
            return joiningInteraction.CanJoin(participant);
        }

        return joiningInteraction.InterruptLowerPriorityInteractions &&
            currentInteraction.Priority < joiningInteraction.Priority &&
            joiningInteraction.CanJoin(participant) &&
            currentInteraction.CanLeave(participant);
    }

    public bool TryJoinInteraction(ACreatureInteraction inter, CreatureData p)
    {
        if (!CanJoin(p, inter)) return false;

        if (participants.TryGetValue(p, out var curr))
        {
            if (waitingToJoin.ContainsKey(p) && waitingToJoin[p] == inter) Debug.Log($"[{p.Identity}] Edge case that was half-expected: HIT when trying to join {inter}");
            if (curr.TryLeave(p))
            {
                waitingToJoin[p] = inter;
                return true;
            }
        }
        
        bool joined = inter.TryJoin(p);
            if (joined)
            {
                participants[p] = inter;
            }
            return joined;
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

    #region Destruction


    #endregion
}
