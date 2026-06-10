using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class DemoCharacterBehaviour : MonoBehaviour
{
    [System.Serializable]
    public class EmotionParameter
    {
        public string parameterName;

        [Range(-1f, 1f)]
        public float initialValue;

        [HideInInspector]
        public float currentValue;
    }

    [System.Serializable]
    public class EmotionDelta
    {
        public string emotionName;
        public float delta;
    }

    [System.Serializable]
    public class InteractionDefinition
    {
        public string name;

        [Tooltip("Animator Trigger parameter")]
        public string triggerName;

        [Min(0f)]
        public float weight = 1f;

        public EmotionDelta[] selfEffects;

        public EmotionDelta[] targetEffects;
    }

    private enum BehaviourMode
    {
        Wandering,
        Interacting,
        UserSelected,
        UserMoveCommand,
        UserInteractionCommand
    }


    [Header("Movement")]

    [SerializeField]
    private float wanderRadius = 10f;

    [SerializeField]
    private Vector2 idleDurationRange = new(2f, 6f);

    [SerializeField]
    private float destinationReachDistance = 0.5f;

    [Header("Interactions")]

    [SerializeField]
    private float interactionRadius = 3f;

    [SerializeField]
    private float interactionCooldown = 10f;

    [SerializeField]
    private float interactionDuration = 2f;

    [SerializeField]
    private float interactionChancePerDecision = 0.6f;

    [SerializeField]
    private InteractionDefinition[] interactions;

    [Header("Emotions")]

    [SerializeField]
    private EmotionParameter[] emotions;

    private static readonly List<DemoCharacterBehaviour> AllCharacters =
        new();

    private NavMeshAgent agent;
    private Animator animator;

    private float idleTimer;
    private float interactionTimer;
    private float nextInteractionAllowedTime;
    private DemoCharacterBehaviour currentPartner;
    private BehaviourMode mode;
    public bool IsBusy => mode != BehaviourMode.Wandering;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        foreach (var emotion in emotions)
        {
            emotion.currentValue = emotion.initialValue;
        }
    }

    private void OnEnable()
    {
        AllCharacters.Add(this);
    }

    private void OnDisable()
    {
        AllCharacters.Remove(this);
    }

    private void Start()
    {
        mode = BehaviourMode.Wandering;
        ChooseNewDestination();
    }

    private void Update()
    {
        UpdateAnimatorEmotions();

        switch (mode)
        {
            case BehaviourMode.Wandering:
                UpdateWandering();
                break;

            case BehaviourMode.Interacting:
                UpdateInteraction();
                break;

            case BehaviourMode.UserSelected:
                UpdateUserSelected();
                break;

            case BehaviourMode.UserMoveCommand:
                UpdateUserMoveCommand();
                break;

            case BehaviourMode.UserInteractionCommand:
                UpdateUserInteractionCommand();
                break;
        }
    }

    private void UpdateWandering()
    {
        if (agent.pathPending)
            return;

        if (agent.remainingDistance > destinationReachDistance)
            return;

        idleTimer -= Time.deltaTime;

        if (idleTimer > 0f)
            return;

        if (Time.time >= nextInteractionAllowedTime)
        {
            TryStartInteraction();
        }

        if (!isInteracting)
        {
            ChooseNewDestination();
        }
    }

    private void UpdateInteraction()
    {
        interactionTimer -= Time.deltaTime;

        if (currentPartner != null)
        {
            FaceTarget(currentPartner.transform.position);
        }

        if (interactionTimer <= 0f)
        {
            EndInteraction();
        }
    }

    private void UpdateUserSelected()
    {
        Camera cam = Camera.main;

        if (cam == null)
            return;

        FaceTarget(
            cam.transform.position,
            userFacingSpeed);
    }

    private void UpdateUserMoveCommand()
    {
        if (agent.pathPending)
            return;

        if (agent.remainingDistance >
            destinationReachDistance)
        {
            return;
        }

        mode = BehaviourMode.Wandering;

        idleTimer = Random.Range(
            idleDurationRange.x,
            idleDurationRange.y);
    }

    private void UpdateUserInteractionCommand()
    {
        if (commandedTarget == null)
        {
            mode = BehaviourMode.Wandering;
            return;
        }

        agent.SetDestination(
            commandedTarget.transform.position);

        float distance =
            Vector3.Distance(
                transform.position,
                commandedTarget.transform.position);

        if (distance > interactionRadius)
            return;

        InteractionDefinition interaction =
            ChooseRandomInteraction();

        if (interaction == null)
        {
            mode = BehaviourMode.Wandering;
            return;
        }

        BeginInteraction(
            commandedTarget,
            interaction);
    }

    private void TryStartInteraction()
    {
        if (Random.value > interactionChancePerDecision)
            return;

        DemoCharacterBehaviour partner = FindNearbyPartner();

        if (partner == null)
            return;

        if (partner.IsBusy)
            return;

        InteractionDefinition interaction =
            ChooseRandomInteraction();

        if (interaction == null)
            return;

        BeginInteraction(partner, interaction);
    }

    private void BeginInteraction(
        DemoCharacterBehaviour partner,
        InteractionDefinition interaction)
    {
        mode = BehaviourMode.Interacting;
        currentPartner = partner;

        interactionTimer = interactionDuration;
        nextInteractionAllowedTime =
            Time.time + interactionCooldown;

        agent.ResetPath();

        partner.ReceiveInteraction(this, interaction);
        commandedTarget = null;
        FaceTarget(partner.transform.position);

        if (!string.IsNullOrWhiteSpace(interaction.triggerName))
        {
            animator.SetTrigger(interaction.triggerName);
        }

        ApplyEmotionEffects(
            interaction.selfEffects,
            this);

        ApplyEmotionEffects(
            interaction.targetEffects,
            partner);
    }

    private void ReceiveInteraction(
        DemoCharacterBehaviour initiator,
        InteractionDefinition interaction)
    {
        mode = BehaviourMode.Interacting;
        currentPartner = initiator;

        interactionTimer = interactionDuration;

        nextInteractionAllowedTime =
            Time.time + interactionCooldown;

        agent.ResetPath();

        FaceTarget(initiator.transform.position);

        if (!string.IsNullOrWhiteSpace(interaction.triggerName))
        {
            animator.SetTrigger(interaction.triggerName);
        }
    }

    private void EndInteraction()
    {
        currentPartner = null;

        mode = BehaviourMode.Wandering;

        ChooseNewDestination();
    }

    public void BeginUserInteraction()
    {
        mode = BehaviourMode.UserSelected;

        commandedTarget = null;

        agent.SetDestination(transform.position);

        if (!string.IsNullOrWhiteSpace(userWaveTrigger))
        {
            animator.SetTrigger(userWaveTrigger);
        }
    }

    public void CancelUserInteraction()
    {
        commandedTarget = null;

        mode = BehaviourMode.Wandering;

        ChooseNewDestination();
    }

    public void CommandMoveTo(Vector3 destination)
    {
        commandedDestination = destination;

        mode = BehaviourMode.UserMoveCommand;

        agent.SetDestination(destination);
    }

    public void CommandInteractWith(
        DemoCharacterBehaviour target)
    {
        if (target == null)
        {
            CancelUserInteraction();
            return;
        }

        commandedTarget = target;

        mode = BehaviourMode.UserInteractionCommand;

        agent.SetDestination(target.transform.position);
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction =
            targetPosition - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                360f * Time.deltaTime);
    }

    private void ChooseNewDestination()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomOffset =
                Random.insideUnitSphere * wanderRadius;

            randomOffset.y = 0f;

            Vector3 candidate =
                transform.position + randomOffset;

            if (NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    wanderRadius,
                    NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);

                idleTimer = Random.Range(
                    idleDurationRange.x,
                    idleDurationRange.y);

                return;
            }
        }
    }

    private DemoCharacterBehaviour FindNearbyPartner()
    {
        DemoCharacterBehaviour best = null;
        float bestDistance = float.MaxValue;

        foreach (var character in AllCharacters)
        {
            if (character == this)
                continue;

            float distance =
                Vector3.Distance(
                    transform.position,
                    character.transform.position);

            if (distance > interactionRadius)
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = character;
            }
        }

        return best;
    }

    private InteractionDefinition ChooseRandomInteraction()
    {
        if (interactions == null ||
            interactions.Length == 0)
        {
            return null;
        }

        float totalWeight = 0f;

        foreach (var interaction in interactions)
        {
            totalWeight += interaction.weight;
        }

        float roll =
            Random.Range(0f, totalWeight);

        foreach (var interaction in interactions)
        {
            roll -= interaction.weight;

            if (roll <= 0f)
            {
                return interaction;
            }
        }

        return interactions[0];
    }

    private static void ApplyEmotionEffects(
        EmotionDelta[] effects,
        DemoCharacterBehaviour character)
    {
        if (effects == null)
            return;

        foreach (var effect in effects)
        {
            foreach (var emotion in character.emotions)
            {
                if (emotion.parameterName ==
                    effect.emotionName)
                {
                    emotion.currentValue =
                        Mathf.Clamp(
                            emotion.currentValue +
                            effect.delta,
                            -1f,
                            1f);

                    break;
                }
            }
        }
    }

    private void UpdateAnimatorEmotions()
    {
        foreach (var emotion in emotions)
        {
            if (string.IsNullOrWhiteSpace(
                    emotion.parameterName))
            {
                continue;
            }

            animator.SetFloat(
                emotion.parameterName,
                emotion.currentValue);
        }
    }
}