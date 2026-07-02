using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Android.Gradle;
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
        DoNothing
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

        timeToRequest = rnd.Range(minReqTime, maxReqTime);

        requestPool[InteractionRequest.CircleDance] = CreateCircleDanceRequest;
        requestPool[InteractionRequest.Wander] = CreateWanderRequest;
        requestPool[InteractionRequest.WalkTo] = CreateWalkToRequest;
        requestPool[InteractionRequest.Follow] = CreateFollowRequest;
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

        if (timer < timeToRequest)
        {
            return;
        }

        timer = 0;

        if (requestPool.Count == 0 || InteractionManager.Creatures.Count == 0)
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
            Debug.Log("Interaction Request UnImplemented for : " + request);
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
            minDistance = 2,
            maxDistance = 10,
            frequency = 2,
            frequencyVariance = 3,
            moveChance = 0.65f
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
}