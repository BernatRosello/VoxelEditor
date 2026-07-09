using UnityEngine;

public class UIUtils
{
    public static void PositionHoverPanel(
    Vector2 mouse,
    Vector2 hoverOffset,
    RectTransform hoverPanel)
{
    Vector2 panelSize = hoverPanel.rect.size;

    Vector2 offset = hoverOffset;

    // Default assumption:
    // mouse is aligned with the top-left corner.

    bool flipHorizontal =
        (mouse.x + offset.x + panelSize.x) > Screen.width;

    bool flipVertical =
        (mouse.y + offset.y - panelSize.y) < 0;

    if (flipHorizontal)
    {
        offset.x *= -1;

        // Move from left side of the mouse to right side
        offset.x -= panelSize.x;
    }

    if (flipVertical)
    {
        offset.y *= -1;

        // Move from above the mouse to below the mouse
        offset.y += panelSize.y;
    }

    hoverPanel.position = mouse + offset;
}
}
