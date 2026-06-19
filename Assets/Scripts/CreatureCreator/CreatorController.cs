using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CreatorController : MonoBehaviour
{
    [SerializeField] GameObject eyes;
    [SerializeField] GameObject head;
    [SerializeField] GameObject torso;
    [SerializeField] GameObject arms;
    [SerializeField] GameObject legs;

    private PlayerControls controls;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        controls = new PlayerControls();

        controls.Player.Click.performed += OnClick;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateHoverUI();
    }

    private void UpdateHoverUI()
    {
        
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        
    }
}
