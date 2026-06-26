using System;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]

public class CameraDragArea : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public bool IsDraggingCamera { get; private set; }
    public float DragThreshold { get => dragThreshold; }
    public bool HasPerssedOnOrbitArea { get; private set; }

    [SerializeField] CinemachineInputAxisController orbitInput;
    private Vector2 pressPosition;
    [SerializeField] private float dragThreshold;

    void Awake()
    {
        orbitInput.enabled = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        HasPerssedOnOrbitArea = true;
        pressPosition = Mouse.current.position.ReadValue();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        HasPerssedOnOrbitArea = false;
        IsDraggingCamera = false;
        
        foreach (var c in orbitInput.Controllers)
        {
            switch (c.Name)
            {
                case "Look Orbit X":
                    c.Input.Gain = 0;
                    break;
                case "Look Orbit Y":
                    c.Input.Gain = 0;
                    break;
                case "Orbit Scale":
                    c.Input.Gain = 1;
                    break;
            }
        }
    }

    void Update()
    {
        if (!IsDraggingCamera && HasPerssedOnOrbitArea)
        {
            float distance = Vector2.Distance(
                pressPosition,
                Mouse.current.position.ReadValue());

            if (distance > DragThreshold)
            {
                IsDraggingCamera = true;
                
                foreach (var c in orbitInput.Controllers)
                {
                    switch (c.Name)
                    {
                        case "Look Orbit X":
                            c.Input.Gain = 1;
                            break;
                        case "Look Orbit Y":
                            c.Input.Gain = -1;
                            break;
                        case "Orbit Scale":
                            c.Input.Gain = 0;
                            break;
                    }
                }
            }
        }
    }
    public bool IsHoveringOrbitArea { get; private set; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsHoveringOrbitArea = true;
        orbitInput.enabled = true;

        if (!IsDraggingCamera)
        foreach (var c in orbitInput.Controllers)
        {
            switch (c.Name)
            {
                case "Look Orbit X":
                    c.Input.Gain = 0;
                    break;
                case "Look Orbit Y":
                    c.Input.Gain = 0;
                    break;
                case "Orbit Scale":
                    c.Input.Gain = 1;
                    break;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsHoveringOrbitArea = false;
        orbitInput.enabled = false;
    }
}