using UnityEngine;
using UnityEngine.InputSystem;

public class MoveToClickPoint : MonoBehaviour
{
    private ActionDriver selectedDriver;
    private PlayerControls playerControls;
    [SerializeField] private bool moveSingleMode = true;

    void Awake()
    {
        selectedDriver = null;//GetComponent<UnityEngine.AI.NavMeshAgent>();

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

        if (!moveSingleMode)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var parameters = new ConversationParams { position = hit.point };
                InteractionManager.CreateRequest(new ConversationInteraction(parameters, InteractionManager.Creatures));
            }
        }
        else if (selectedDriver == null)
        {
            // Select selectedDriver with raycast (if clicked it is set as selectedDriver)
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                selectedDriver = hit.transform.GetComponent<ActionDriver>();
            }
        }
        else
        {
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // WARNING this will probably crop up weird BUGS because of Undefined Behavior
                // if the selectedDriver was already busy with and action.
                // To achieve this appropriately it should be implemented with an interaction.
                // Something like: UserMove that allows selecting a series (or one, in this case)
                // of creatures to move to a given location with a very high priority, in a way that shows-off
                // the functioning of the interaction manager scheduling
                // selectedDriver.SetDestination(hit.point);
                // selectedDriver = null;
                var parameters = new UserMoveParams { position = hit.point };
                InteractionManager.CreateRequest(new ConversationInteraction(parameters, selectedDriver.GetComponent<Creature>().Identity));
                selectedDriver = null;
            }
        }

    }
}