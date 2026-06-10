using UnityEngine;

public class UserInteractionController : MonoBehaviour
{
    public static UserInteractionController Instance { get; private set; }

    [SerializeField]
    private Camera interactionCamera;

    [SerializeField]
    private LayerMask characterMask = ~0;

    [SerializeField]
    private LayerMask worldMask = ~0;

    [SerializeField]
    private float interactionTimeout = 5f;

    private DemoCharacterBehaviour selectedCharacter;
    private float timeoutTimer;

    public DemoCharacterBehaviour SelectedCharacter =>
        selectedCharacter;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }
    }

    private void Update()
    {
        UpdateTimeout();

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void UpdateTimeout()
    {
        if (selectedCharacter == null)
            return;

        timeoutTimer -= Time.deltaTime;

        if (timeoutTimer > 0f)
            return;

        selectedCharacter.CancelUserInteraction();
        selectedCharacter = null;
    }

    private void HandleClick()
    {
        Ray ray =
            interactionCamera.ScreenPointToRay(
                Input.mousePosition);

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                1000f,
                characterMask | worldMask))
        {
            return;
        }

        DemoCharacterBehaviour clickedCharacter =
            hit.collider.GetComponentInParent<
                DemoCharacterBehaviour>();

        if (selectedCharacter == null)
        {
            if (clickedCharacter == null)
                return;

            SelectCharacter(clickedCharacter);
            return;
        }

        timeoutTimer = interactionTimeout;

        if (clickedCharacter != null)
        {
            if (clickedCharacter == selectedCharacter)
                return;

            selectedCharacter.CommandInteractWith(
                clickedCharacter);

            selectedCharacter = null;

            return;
        }

        selectedCharacter.CommandMoveTo(
            hit.point);

        selectedCharacter = null;
    }

    private void SelectCharacter(
        DemoCharacterBehaviour character)
    {
        selectedCharacter = character;

        timeoutTimer = interactionTimeout;

        character.BeginUserInteraction();
    }
}