using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MoveToClickPoint : MonoBehaviour
{
    public enum RequestMode
    {
        None,
        UserMove,
        Dance,
        Wander
    }

    [Header("Settings")]
    [SerializeField] private RequestMode activeRequest;

    private readonly HashSet<Creature> selectedCreatures = new();

    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();

        controls.Player.Click.performed += OnClick;
    }

    private void OnEnable()
    {
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
            Select(hit);
            return;
        }

        // Ctrl + Click
        if (removeMode)
        {
            Deselect(hit);
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

        return Physics.Raycast(ray, out hit, 100f);
    }

    private void Select(RaycastHit hit)
    {
        if (!hit.transform.TryGetComponent(out Creature creature))
            return;

        selectedCreatures.Add(creature);

        Debug.Log($"Selected {creature.name}");
    }

    private void Deselect(RaycastHit hit)
    {
        if (!hit.transform.TryGetComponent(out Creature creature))
            return;

        selectedCreatures.Remove(creature);

        Debug.Log($"Deselected {creature.name}");
    }

    private void CreateRequest(RaycastHit hit)
    {
        if (activeRequest == RequestMode.None)
            return;

        int minimumSelectionCount;
        switch(activeRequest)
        {
            case RequestMode.UserMove:
                break;

            case RequestMode.Dance:
                break;

            case RequestMode.Wander:
                break;
        }

        if (selectedCreatures.Count < minimumSelectionCount)
        {
            Debug.Log($"Cannot create {activeRequest} request. Need at least {minimumSelectionCount} selected creatures.");
            return;
        }

        switch (activeRequest)
        {
            case RequestMode.UserMove:
                var moveParams = new UserMoveParams { position = hit.point };
                var moveRequest = new UserMoveRequest(moveParams, selectedCreatures);
                InteractionManager.CreateRequest(moveRequest);
                break;

            case RequestMode.Dance:
                var danceParams = new CircleDanceParams { position = hit.point };
                var danceRequest = new CircleDanceRequest(danceParams, selectedCreatures);
                InteractionManager.CreateRequest(danceRequest);
                break;

            case RequestMode.Wander:
                var wanderRequest = new WanderRequest(selectedCreatures);
                InteractionManager.CreateRequest(wanderRequest);
                break;
        }

        // Request successfully created -> clear selection
        selectedCreatures.Clear();
    }
}