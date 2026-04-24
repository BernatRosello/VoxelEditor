using Unity.Cinemachine;
using UnityEngine;

public class RotationComposerOffsetter : MonoBehaviour
{
    public RectTransform offsetTarget; // UI panel
    public CinemachineRotationComposer rotationComposer;
    public Vector2 adjustment;

    void OnEnable()
    {
        ApplyOffset(); // ✅ safe init (Android case)

        Canvas.willRenderCanvases += ApplyOffset; // ✅ event-driven refresh
    }

    void OnDisable()
    {
        Canvas.willRenderCanvases -= ApplyOffset;
    }

    void ApplyOffset()
    {
        if (!offsetTarget)
        {
            Debug.LogWarning("No Offset Target bound!");
            return;
        }

        if (!rotationComposer)
        {
            Debug.LogWarning("No Rotation Composer bound!");
            return;
        }
        var parentTransform = offsetTarget.parent.GetComponent<RectTransform>();
        rotationComposer.Composition.ScreenPosition = new Vector2(offsetTarget.rect.width/parentTransform.rect.width/2 - adjustment.x, adjustment.y);
    }
}
