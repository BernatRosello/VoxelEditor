using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using rnd = UnityEngine.Random;

public class DemoCharacterBehaviour : MonoBehaviour
{
    public enum InteractionRequest
    {
        Wander,
        CircleDance,
        Follow,
        WalkTo,
        Idle,
        DoNothing,
        Talk,
        TryJoinRandomInteraction
    }
    [System.Serializable]
    public class InteractionRequestWeight
    {
        public InteractionRequest request;
        [Min(0f)]
        public float weight = 1f;
    }

    [SerializeField] private List<InteractionRequestWeight> interactionWeights = new();
    [SerializeField] private float minReqTime;
    [SerializeField] private float maxReqTime;

    private float timer;
    private float timeToRequest;
    private readonly Dictionary<InteractionRequest, Action> requestPool = new();

    private void Awake()
    {
        timer = 0;

        timeToRequest = rnd.Range(minReqTime/InteractionManager.Creatures.Count, maxReqTime/InteractionManager.Creatures.Count);

        requestPool[InteractionRequest.CircleDance] = CreateCircleDanceRequest;
        requestPool[InteractionRequest.Wander] = CreateWanderRequest;
        requestPool[InteractionRequest.WalkTo] = CreateWalkToRequest;
        requestPool[InteractionRequest.Follow] = CreateFollowRequest;
        requestPool[InteractionRequest.Idle] = CreateIdleRequest;
        requestPool[InteractionRequest.Talk] = CreateTalkRequest;
        requestPool[InteractionRequest.TryJoinRandomInteraction] = TryJoinRandomOngoing;
    }

    private void OnValidate()
    {
        foreach (InteractionRequest request in Enum.GetValues(typeof(InteractionRequest)))
        {
            if (!interactionWeights.Any(x => x.request == request))
            {
                interactionWeights.Add(new InteractionRequestWeight
                {
                    request = request,
                    weight = 1f
                });
            }
        }

        interactionWeights.Sort((a, b) => a.request.CompareTo(b.request));
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if ((timer < timeToRequest) || (maxReqTime <= 0))
        {
            return;
        }

        timer = 0;
        timeToRequest = rnd.Range(minReqTime/InteractionManager.Creatures.Count, maxReqTime/InteractionManager.Creatures.Count);


        float totalWeight = 0f;
        foreach (var entry in interactionWeights)
        {
            totalWeight += entry.weight >= 0 ? entry.weight : 0;
        }

        if (requestPool.Count == 0 || InteractionManager.Creatures.Count == 0 || totalWeight == 0)
        {
            return;
        }

        InteractionRequest request = GetRandomInteraction();

        if (requestPool.TryGetValue(request, out Action action))
        {
            action();
        }
        else
        {
            Debug.LogWarning("Interaction Request UnImplemented for : " + request);
        }
    }
    private InteractionRequest GetRandomInteraction()
    {
        float totalWeight = 0f;

        foreach (var entry in interactionWeights)
            totalWeight += entry.weight;

        if (totalWeight <= 0f)
            throw new InvalidOperationException("No interaction requests have a positive weight.");

        float random = Random.value * totalWeight;

        foreach (var entry in interactionWeights)
        {
            random -= entry.weight;

            if (random <= 0f)
                return entry.request;
        }

        // Floating-point safety.
        return interactionWeights[^1].request;
    }

    private Vector3? GetRandomNavmeshPosition()
    {
        if (NavMeshUtility.TryGetRandomPosition(Vector3.zero, 50, 8, out Vector3 vec))
            return vec;
        else
            return null;
    }

    private void CreateCircleDanceRequest()
    {
        CircleDanceInteraction temp = new(null, null);

        int count = rnd.Range(temp.MinParticipants, temp.MaxParticipants + 1);
        count = Math.Clamp(count, 1, InteractionManager.Creatures.Count);
        List<CreatureIdentity> candidates = InteractionManager.Creatures.OrderBy(_ => rnd.value).Take(count).ToList();

        CircleDanceParams p = new()
        {
            position = Random.Range(0, 1f) > 0.5f ? GetRandomNavmeshPosition() : null,
            initiator = candidates.First(),
            duration = rnd.Range(5f, 15f)
        };

        InteractionManager.CreateRequest(new CircleDanceRequest(p, candidates.AsEnumerable()));
    }

    private void CreateWanderRequest()
    {
        CreatureIdentity candidate = InteractionManager.Creatures.OrderBy(_ => rnd.value).First();
        WanderingParams p = new()
        {
            duration = rnd.value * 30,
            minDistance = 3,
            maxDistance = 15,
            frequency = 3,
            frequencyVariance = 2.5f,
            moveChance = 0.5f
        };

        InteractionManager.CreateRequest(new WanderingRequest(p, candidate));
    }

    private void CreateWalkToRequest()
    {
    }

    private void CreateFollowRequest()
    {
        var candidates = InteractionManager.Creatures.OrderBy(_ => rnd.value).Take(2);
        var followTransform = InteractionManager.TryGetCreatureData(candidates.Last()).Driver.transform;

        FollowTransformParams p = new()
        {
            followTarget = followTransform,
            duration = rnd.value * 40
        };

        InteractionManager.CreateRequest(new FollowTransformRequest(p, candidates.First()));
    }

    private void CreateIdleRequest()
    {
        CreatureIdentity candidate = InteractionManager.Creatures.OrderBy(_ => rnd.value).First();

        IdleParams p = new()
        {
            duration = 120
        };

        InteractionManager.CreateRequest(new IdleInteractionRequest(p, candidate));
    }

    private void CreateTalkRequest()
    {
        TalkInteraction temp = new(null, null);

        int count = rnd.Range(temp.MinParticipants, temp.MaxParticipants + 1);
        count = Math.Clamp(count, 1, InteractionManager.Creatures.Count);
        List<CreatureIdentity> candidates = InteractionManager.Creatures.OrderBy(_ => rnd.value).Take(count).ToList();

        TalkParams p = new()
        {
            duration = rnd.Range(6f, 40f)
        };

        InteractionManager.CreateRequest(new TalkInteractionRequest(p, candidates.AsEnumerable()));
    }

    private void TryJoinRandomOngoing()
    {
        if (InteractionManager.Interactions.Count == 0) return;

        var p = InteractionManager.TryGetCreatureData(InteractionManager.Creatures.OrderBy(_ => rnd.value).First());
        var ongoing = InteractionManager.Interactions.OrderBy(_ => rnd.value).First();
        var allowed = InteractionManager.Instance.TryJoinInteraction(ongoing, p);
        
        // Debug.Log($"Creature({p.Identity}) attempted to join Interaction({ongoing})");
    }
}