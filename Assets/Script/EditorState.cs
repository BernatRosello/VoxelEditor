using UnityEngine;


public class EditorState : MonoBehaviour
{
    public static EditorState Instance;


    public int activeOrientation = 0;
    public int activeReflection = 0;
    public VoxelMeshID activeMesh;
    public static readonly Quaternion[] ROTS = new Quaternion[8]
    {
        // 0 (+,+,-)
        Quaternion.Euler(180, 0, -90),
        // 1 (+,+,+) ← canonical
        Quaternion.identity,
        // 2 (-,+,+)
        Quaternion.Euler(0, 0, 90),
        // 3 (-,+,-)
        Quaternion.Euler(180, 0, 180),
        // 4 (+,-,-)
        Quaternion.Euler(180, 0, 0),
        // 5 (+,-,+)
        Quaternion.Euler(0, 0, -90),
        // 6 (-,-,+)
        Quaternion.Euler(0, 0, 180),
        // 7 (-,-,-)
        Quaternion.Euler(180, 0, 90),
    };
    public System.Action OnStateChanged;

    void Awake()
    {
        Instance = this;
    }
}
