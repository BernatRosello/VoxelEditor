using TMPro;
using UnityEngine;

public class CreatureEditorManager : MonoBehaviour
{
    public static CreatureEditorManager Instance;
    [SerializeField] private MaterialLibrary materialLibrary;

    public static MaterialLibrary MaterialLibrary { get => Instance.materialLibrary; }

    [SerializeField] private Transform shapeContentRoot;

    [SerializeField] private GameObject shapeSliderPrefab;

    [SerializeField] private TMP_Dropdown bodyPartDropdown;

    [SerializeField] private TMP_Dropdown materialDropdown;

    [SerializeField] private FlexibleColorPicker colorPicker;

    // BodyMesh currently being edited
    private BodyMesh currentBody;

    // Visuals currently being edited
    private CreatureVisuals currentVisuals;

    private enum Tab
    {
        Shape,
        Texture
    }

    private enum BodyPart
    {
        Head,
        Eyes,
        Torso,
        Arms,
        Legs
    }

    private BodyPart selectedBodyPart;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }



#if UNITY_EDITOR

    private void OnValidate()
    {
        Awake();
    }
#endif

    public static Material GetMaterial(CreatureVisuals.MaterialOption mat)
    {
        switch (mat)
        {
            case CreatureVisuals.MaterialOption.Fur:
                return MaterialLibrary.FurMaterial;
            case CreatureVisuals.MaterialOption.BSDF:
                return MaterialLibrary.BSDF;
            default:
                return null;
        }
    }
}