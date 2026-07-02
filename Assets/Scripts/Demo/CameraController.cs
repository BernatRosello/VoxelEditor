using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform cameraFollowTarget;
    [SerializeField] private float speed;
    private PlayerControls controls;
    private Vector3 move;

    void Awake()
    {

        controls = new PlayerControls();
    }

    void OnEnable()
    {
        controls.Player.Enable();
        controls.Player.Move.performed += OnStartMove;
        controls.Player.Move.canceled += OnStopMove;
    }

    void OnDisable()
    {
        controls.Player.Move.performed -= OnStartMove;
        controls.Player.Move.canceled -= OnStopMove;
        controls.Player.Disable();
    }

    private void OnStartMove(InputAction.CallbackContext context)
    {
        var vec = context.ReadValue<Vector2>();
        move.x = vec.x;
        move.z = vec.y;
    }

    private void OnStopMove(InputAction.CallbackContext context)
    {
        move = Vector2.zero;
    }

    void Update()
    {
        Vector3 forward = Vector3.ProjectOnPlane(cameraFollowTarget.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraFollowTarget.right, Vector3.up).normalized;

        Vector3 worldMove = right * move.x + forward * move.z;
        cameraFollowTarget.position += worldMove.normalized * speed * Time.deltaTime;
    }
}