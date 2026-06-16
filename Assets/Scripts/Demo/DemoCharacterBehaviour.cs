using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using rnd = UnityEngine.Random;

public class DemoCharacterBehaviour : MonoBehaviour
{
    [SerializeField] private float minReqTime;
    [SerializeField] private float maxReqTime;

    private float timer;
    private float timeToRequest;
    private readonly List<Action> requestPool = new();

    private void Awake()
    {
        timer = 0;

        timeToRequest = rnd.Range(minReqTime, maxReqTime);

        requestPool.Add(CreateCircleDanceRequest);

        // requestPool.Add(CreateDanceRequest);
        // requestPool.Add(CreateGroupDanceRequest);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer < timeToRequest)
        {
            return;
        }

        timer = 0;

        timeToRequest = rnd.Range(minReqTime, maxReqTime);

        if (requestPool.Count == 0)
        {
            return;
        }

        requestPool[
            rnd.Range(0, requestPool.Count)
        ]();
    }

    private void CreateCircleDanceRequest()
    {
        if (InteractionManager.Creatures.Count == 0)
        {
            return;
        }

        CircleDanceInteraction temp = new(null, null);

        int count = rnd.Range(temp.MinParticipants, temp.MaxParticipants + 1);
        count = Math.Clamp(count, 1, InteractionManager.Creatures.Count);
        List<CreatureIdentity> candidates = InteractionManager.Creatures.OrderBy(_ => rnd.value).Take(count).ToList();

        CircleDanceParams p = new()
        {
            initiator = candidates.First(),
            duration = rnd.Range(5f, 15f)
        };

        InteractionManager.CreateRequest(new CircleDanceRequest(p, candidates.AsEnumerable()));
    }

    /*
    private void CreateDanceRequest()
    {
        ...
    }

    private void CreateGroupDanceRequest()
    {
        ...
    }
    */
}