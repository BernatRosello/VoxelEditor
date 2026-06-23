using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]

public class CameraDragArea : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    public bool IsDraggingCamera { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsDraggingCamera = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsDraggingCamera = false;
    }

    void Update()
    {
        if (IsDraggingCamera &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            IsDraggingCamera = false;
        }
    }
}