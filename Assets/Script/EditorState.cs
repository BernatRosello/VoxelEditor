using UnityEngine;


public class EditorState : MonoBehaviour
{
    public static EditorState Instance;


    public int activeOrientation = 0;
    public int activeReflection = 0;
    public VoxelMeshID activeMesh;
    public static readonly Quaternion[] ROTS = new Quaternion[8]
    {
        // 0 (-,-,-)
        Quaternion.Euler(180, 0, 90),

        // 1 (+,-,-)
        Quaternion.Euler(180, 0, 0),

        // 2 (-,+,-)
        Quaternion.Euler(180, 0, 180),

        // 3 (+,+,-)
        Quaternion.Euler(180, 0, -90),

        // 4 (-,-,+)
        Quaternion.Euler(0, 0, 180),

        // 5 (+,-,+)
        Quaternion.Euler(0, 0, -90),

        // 6 (-,+,+)
        Quaternion.Euler(0, 0, 90),

        // 7 (+,+,+) ← canonical
        Quaternion.identity
    };

    void Awake()
    {
        Instance = this;
    }
}
