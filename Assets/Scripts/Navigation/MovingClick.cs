using UnityEngine;
using UnityEngine.InputSystem;

public class MoveToClickPoint : MonoBehaviour
{
    private UnityEngine.AI.NavMeshAgent agent;
    private PlayerControls playerControls;

    void Awake()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        playerControls = new PlayerControls();

        playerControls.Player.Clicked.performed += OnClick;
    }

    void OnEnable()
    {
        playerControls.Player.Enable();
    }

    void OnDisable()
    {
        playerControls.Player.Disable();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        Vector2 mousePosition = context.ReadValue<Vector2>();

        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            agent.destination = hit.point;
        }
    }
}