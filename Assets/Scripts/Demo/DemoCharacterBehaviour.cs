using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class DemoCharacterBehaviour : MonoBehaviour
{
    private float timer;

    private float timeToRequest;

    private readonly List<Action> requestPool = new();

    private void Awake()
    {
        identity = GetComponent<CreatureIdentity>();

        timer = 0;

        timeToRequest = requestTime.Value;

        requestPool.Add(CreateConversationRequest);

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

        timeToRequest = requestTime.Value;

        if (requestPool.Count == 0)
        {
            return;
        }

        requestPool[
            Random.Range(0, requestPool.Count)
        ]();
    }

    private void CreateConversationRequest()
    {
        if (InteractionManager.Creatures.Count == 0)
        {
            return;
        }

        int count = Random.Range(ConversationInteraction.MinParticipants, ConversationInteraction.MaxParticipants + 1);
        count = Math.Clamp(count, 1, InteractionManager.Creatures.Count);
        List<CreatureIdentity> candidates = InteractionManager.Creatures.OrderBy(_ => Random.value).Take(count).ToList();

        ConversationParams p = new()
        {
            initiator = identity,
            duration = Random.Range(5f, 15f)
        };

        InteractionManager.Instance.RequestInteraction(new ConversationRequest(p, new[] { identity, candidates }));
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