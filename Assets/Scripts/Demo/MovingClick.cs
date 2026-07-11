using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting;

public class MoveToClickPoint : MonoBehaviour
{
    public enum RequestMode
    {
        None,
        UserMove,
        Dance,
        Wander,
        Idle,
        Talk
    }

    [SerializeField] private RequestMode activeRequest;
    [Header("Settings")]
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private LayerMask raycastMask;
    [Header("Hover UI")]
    [SerializeField] private RectTransform hoverPanel;
    [SerializeField] private TMP_Text hoverText;
    [SerializeField] private Vector2 hoverOffset = new(20, -20);

    private readonly HashSet<Creature> selectedCreatures = new();

    private PlayerControls controls;
    [SerializeField] private List<int> minimumSelected = new();
    private List<ActionDriver> outlinedCreatures = new();

    [SerializeField] private int m_avgEmotesPerMinute = 5;

    private void Awake()
    {
        controls = new PlayerControls();

        controls.Player.Click.performed += OnClick;
    }

    private void Update()
    {
        UpdateOutlines();
        UpdateHoverUI();
    }

    private void UpdateHoverUI()
    {
        ActionDriver hoveredDriver = GetHoveredCreatureDriver();

        if (hoveredDriver == null)
        {
            hoverPanel.gameObject.SetActive(false);
            return;
        }

        hoverPanel.gameObject.SetActive(true);

        Vector2 mouse = controls.Player.Point.ReadValue<Vector2>();
        UIUtils.PositionHoverPanel(mouse, hoverOffset, hoverPanel);

        Creature creature = hoveredDriver.GetComponent<Creature>();

        if (creature == null)
        {
            SetHoverText(null);
            return;
        }

        CreatureData creatureData =
            InteractionManager.TryGetCreatureData(creature.Identity);

        ACreatureInteraction interaction =
            InteractionManager.TryGetInteraction(creatureData);

        SetHoverText(interaction, creatureData);
    }

    private void SetHoverText(ACreatureInteraction interaction, CreatureData creature = null)
    {
        if (interaction == null)
        {
            hoverText.text =
$@"Interaction
  Interaction Priority: -
  N_Participants: -

Participant[{creature.Identity}] State
  Phase: -
  Action Index: -
  Action Complete: -";

            return;
        }

        CreatureInteractionState state = interaction.TryReadState(creature);

        hoverText.text =
$@"{interaction.Name} Interaction
  Interaction Priority: {interaction.Priority}
  N_Participants: {interaction.ActiveParticipants.Count}
  {(interaction.DebugInfo != "" ? $"Debug Info: {interaction.DebugInfo}" : "")}

Participant[{creature.Identity}] State
  Phase: {state?.Phase ?? 0}
  Action Index: {state?.ActionIndex ?? -1}
  Action Complete: {state?.ActionComplete ?? false}";
    }

    private void UpdateOutlines()
    {
        ActionDriver hovered = GetHoveredCreatureDriver();

        bool adding = controls.Player.Select.IsPressed();
        bool removing = controls.Player.Remove.IsPressed();

        HashSet<ActionDriver> currentlyRelevant = new(
            selectedCreatures
                .Select(c => c.GetComponent<ActionDriver>())
                .Where(ad => ad != null)
        );

        if (hovered != null)
            currentlyRelevant.Add(hovered);

        // Remove outlines from creatures that are no longer relevant
        foreach (ActionDriver creature in outlinedCreatures.Except(currentlyRelevant).ToList())
        {
            creature.SetOutlineNONE();
        }

        // Apply current outlines
        foreach (ActionDriver creature in currentlyRelevant)
        {
            bool isHovered = creature == hovered;
            bool isSelected = selectedCreatures.Contains(creature.GetComponent<Creature>());

            // Hover modifiers have priority
            if (removing && isHovered)
            {
                creature.SetOutlineRed();
            }
            else if (adding && isHovered)
            {
                creature.SetOutlineGreen();
            }
            else if (isSelected)
            {
                creature.SetOutlineBlue();
            }
            else
            {
                creature.SetOutlineNONE();
            }
        }

        outlinedCreatures.Clear();

        foreach (ActionDriver creature in currentlyRelevant)
        {
            outlinedCreatures.Add(creature);
        }
    }

    private ActionDriver GetHoveredCreatureDriver()
    {
        if (!TryRaycast(out RaycastHit hit))
            return null;



        Creature c = GetCreature(hit); hit.transform.TryGetComponent(out BodyPartCollider body);
        if (c == null) return null;

        c.TryGetComponent(out ActionDriver creatureDriver);
        // Debug.Log($"Fetched CreatureDriver {creatureDriver} from {body.creature.Identity}'s {body.BodyPart}");
        return creatureDriver;
    }

    private void OnEnable()
    {
        // Callback added inside Awake()
        controls.Player.Enable();
    }
    private void OnDisable()
    {
        controls.Player.Click.performed -= OnClick;
        controls.Player.Disable();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        if (!TryRaycast(out RaycastHit hit))
            return;

        // Modifier actions defined in Input System
        bool addMode = controls.Player.Select.IsPressed();
        bool removeMode = controls.Player.Remove.IsPressed();

        // Shift + Click
        if (addMode)
        {
            Select(GetCreature(hit));
            return;
        }

        // Ctrl + Click
        if (removeMode)
        {
            Deselect(GetCreature(hit));
            return;
        }

        // Normal click
        CreateRequest(hit);
    }

    private bool TryRaycast(out RaycastHit hit)
    {
        hit = default;
        Vector2 mousePosition = controls.Player.Point.ReadValue<Vector2>();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        return Physics.Raycast(ray, out hit, raycastDistance, raycastMask);
    }

    private Creature GetCreature(RaycastHit hit)
    {
        hit.transform.TryGetComponent(out BodyPartCollider body);
        if (body == null)
        {
            // Debug.Log($"Failed to hit valid BodyPartCollider, instead hit: {hit.transform.gameObject}");
            return null;
        }
        return body.creature;
    }

    private void Select(Creature creature)
    {
        if (creature == null)
            return;

        selectedCreatures.Add(creature);

        // Debug.Log($"Selected {creature.name}");
    }

    private void Deselect(Creature creature)
    {
        if (creature == null)
            return;

        selectedCreatures.Remove(creature);

        // Debug.Log($"Deselected {creature.name}");
    }

    private void CreateRequest(RaycastHit hit)
    {
        if (activeRequest == RequestMode.None)
            return;

        int minimumSelectionCount = minimumSelected[(int)activeRequest - 1];

        if (selectedCreatures.Count < minimumSelectionCount)
        {
            Debug.Log($"Cannot create {activeRequest} request. Need at least {minimumSelectionCount} selected creatures.");
            return;
        }

        switch (activeRequest)
        {
            case RequestMode.UserMove:
                var moveParams = new UserMoveParams { position = hit.point };
                foreach (var c in selectedCreatures)
                {
                    var moveRequest = new UserMoveRequest(moveParams, c.Identity);
                    InteractionManager.CreateRequest(moveRequest);
                }
                break;

            case RequestMode.Dance:
                var danceParams = new CircleDanceParams { position = hit.point };
                var danceRequest = new CircleDanceRequest(danceParams, selectedCreatures.Select(c => c.Identity));
                InteractionManager.CreateRequest(danceRequest);
                break;

            case RequestMode.Wander:
                var wanderParams = new WanderingParams { duration = 90, frequency = 5, frequencyVariance = 4, maxDistance = 15, minDistance = 2, moveChance = 0.5f };
                foreach (var c in selectedCreatures)
                {
                    var wanderRequest = new WanderingRequest(wanderParams, c.Identity);
                    InteractionManager.CreateRequest(wanderRequest);
                }
                break;

            case RequestMode.Idle:

                IdleParams idleParams = new()
                {
                    duration = 30,
                    avgEmotesPerMinute = m_avgEmotesPerMinute
                };
                foreach (var c in selectedCreatures)
                {
                    InteractionManager.CreateRequest(new IdleInteractionRequest(idleParams, c.Identity));
                }
                break;

            case RequestMode.Talk:

                TalkParams talkParams = new()
                {
                    duration = 20,
                };
                InteractionManager.CreateRequest(new TalkInteractionRequest(talkParams, selectedCreatures.Select(c => c.Identity)));
                break;

        }

        // Request successfully created -> clear selection
        selectedCreatures.Clear();
    }
}