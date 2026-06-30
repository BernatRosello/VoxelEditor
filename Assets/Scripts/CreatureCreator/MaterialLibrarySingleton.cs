using UnityEngine;

public class MaterialLibrarySingleton : MonoBehaviour
{
    private static MaterialLibrarySingleton Instance;
    [SerializeField] private MaterialLibrary m_materialLibrary;

    private static MaterialLibrary MaterialLibrary { get => Instance.m_materialLibrary; set => Instance.m_materialLibrary = value; }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }
#endif

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }


    public static Material GetMaterial(CreatureVisuals.MaterialOption mat)
    {
        switch (mat)
        {
            case CreatureVisuals.MaterialOption.Fur:
                return MaterialLibrary.FurMaterial;
            case CreatureVisuals.MaterialOption.OpaqueBSDF:
                return MaterialLibrary.OpaqueBSDF;
            case CreatureVisuals.MaterialOption.TransparentBSDF:
                return MaterialLibrary.TransparentBSDF;
            default:
                return null;
        }
    }
}