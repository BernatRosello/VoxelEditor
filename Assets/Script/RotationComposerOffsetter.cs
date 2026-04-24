using Unity.Cinemachine;
using UnityEngine;

public class RotationComposerOffsetter : MonoBehaviour
{
    public RectTransform offsetTarget;
    public CinemachineRotationComposer rotationComposer;
    

    // Update is called once per frame
    void Update()
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

        // TODO NORMALIZE!!!
        rotationComposer.TargetOffset = offsetTarget.position;
    }
}
