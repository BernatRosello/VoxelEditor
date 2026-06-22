using System;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CreatureEditorManager : MonoBehaviour
{
    public static CreatureEditorManager Instance;
    [SerializeField] private MaterialLibrary materialLibrary;

    public static MaterialLibrary MaterialLibrary { get => Instance.materialLibrary; }
    public CreatureVisuals CurrentVisuals { get => currentCreature.Visuals; }
    public BodyMesh CurrentBodyMesh { get => currentCreature.BodyMesh; }

    [SerializeField] private Toggle shapeToggle;
    [SerializeField] private Toggle textureToggle;
    [SerializeField] private ScrollRect shapeScrollRect;
    [SerializeField] private Transform shapeContentRoot;
    [SerializeField] private Transform materialPropertiesRoot;
    [SerializeField] private GameObject shapeSliderPrefab;
    [SerializeField] private Transform shapeTab;
    [SerializeField] private Transform textureTab;
    [SerializeField] private TMP_Dropdown materialDropdown;
    [SerializeField] private FlexibleColorPicker colorPicker;
    [SerializeField] private TMP_Text bodyPartText;
    [SerializeField] private BodyPart selectedBodyPart;
    [SerializeField] private Creature currentCreature;
    private PlayerControls controls;

    private enum Tab
    {
        Shape,
        Texture
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }

        controls = new PlayerControls();

        controls.Player.Click.performed += OnClick;
        // Bind tab toggles
        shapeToggle.onValueChanged.AddListener(_ => OnTabChanged());
        textureToggle.onValueChanged.AddListener(_ => OnTabChanged());

        shapeToggle.SetIsOnWithoutNotify(true);

        OnTabChanged();

        BeginEditing(FindAnyObjectByType<Creature>());
    }

    private void OnEnable()
    {
        controls.Player.Click.performed += OnClick;
        controls.Player.Enable();
    }
    private void OnDisable()
    {
        controls.Player.Click.performed -= OnClick;
        controls.Player.Disable();
    }

    private void OnTabChanged()
    {
        bool tabToShape = shapeToggle.isOn;

        if (tabToShape)
        {

        }
        else // Tab to Texture
        {
            colorPicker.StartingColor = CurrentVisuals.GetColor(selectedBodyPart);
        }

        shapeTab.gameObject.SetActive(tabToShape);

        textureTab.gameObject.SetActive(!tabToShape);
    }



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

    private void OnClick(InputAction.CallbackContext context)
    {
        Vector2 mousePosition = controls.Player.Point.ReadValue<Vector2>();

        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(ray, out var hit, 30f))
            return;

        var bodyPartCollider = hit.collider.GetComponent<BodyPartCollider>();

        if (bodyPartCollider == null)
            return;

        var creature = hit.transform.GetComponentInParent<Creature>();
        if (creature != currentCreature)
        {
            Debug.Log("Changing Selected Creature");
            SetCurrentCreature(creature);
        }

        SelectBodyPart(bodyPartCollider.BodyPart);
    }

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

    public void BeginEditing(Creature creature)
    {
        SetCurrentCreature(creature);

        SelectBodyPart(BodyPart.Head);

        BuildTextureUI();
    }

    private void SetCurrentCreature(Creature creature)
    {
        if (currentCreature != null && currentCreature != creature)
        {
            currentCreature.DisableBodyPartColliders();
        }
        currentCreature = creature;
        currentCreature.EnableBodyPartColliders();
    }

    private void BuildTextureUI()
    {
        materialDropdown.ClearOptions();

        materialDropdown.AddOptions(Enum.GetNames(typeof(CreatureVisuals.MaterialOption)).ToList());
        RebindTextureTab();
    }

    public static void SelectBodyPart(BodyPart bodyPart)
    {
        Instance.SetSelectBodyPart(bodyPart);
    }

    private void SetSelectBodyPart(BodyPart bodyPart)
    {
        selectedBodyPart = bodyPart;

        bodyPartText.text = bodyPart.ToString();

        RebuildShapeTab();

        RebindTextureTab();
    }

    private void RebindTextureTab()
    {
        materialDropdown.onValueChanged.RemoveAllListeners();
        colorPicker.onColorChange.RemoveAllListeners();

        materialDropdown.SetValueWithoutNotify((int)CurrentVisuals.GetMaterialSettings(selectedBodyPart).type);
        colorPicker.SetColor(CurrentVisuals.GetColor(selectedBodyPart));

        materialDropdown.onValueChanged.AddListener(OnMaterialChanged);
        colorPicker.onColorChange.AddListener(OnColorChanged);

        RebuildMaterialProperties();
    }

    private void OnMaterialChanged(int index)
    {
        var matSettings = CurrentVisuals.GetMaterialSettings(selectedBodyPart);
        matSettings.type = (CreatureVisuals.MaterialOption)index;
        CurrentVisuals.SetMaterialSettings(selectedBodyPart, matSettings);
        CreatureVisuals.ApplyMaterials(CurrentBodyMesh, CurrentVisuals);
        RebuildMaterialProperties();
    }

    private void OnColorChanged(Color color)
    {
        CurrentVisuals.SetColor(selectedBodyPart, color);
        CreatureVisuals.ApplyColors(CurrentBodyMesh, CurrentVisuals);
    }

    private void RebuildMaterialProperties()
    {
        foreach (Transform child in materialPropertiesRoot)
        {
            if (child.GetComponent<SliderUI>() != null)
                Destroy(child.gameObject);
        }

        var materialSettings = CurrentVisuals.GetMaterialSettings(selectedBodyPart);
        object settings = materialSettings.type == CreatureVisuals.MaterialOption.Fur ? materialSettings.furSettings : materialSettings.bsdfSettings;


        var fields = settings.GetType().GetFields();

        foreach (var field in fields)
        {
            if (field.IsLiteral)
                continue;

            if (field.FieldType != typeof(float))
                continue;

            GameObject go = Instantiate(shapeSliderPrefab, materialPropertiesRoot);
            TMP_Text label = go.GetComponentInChildren<TMP_Text>();
            Slider slider = go.GetComponentInChildren<Slider>();
            label.text = field.Name;
            RangeAttribute range = field.GetCustomAttribute<RangeAttribute>();

            if (range != null)
            {
                if (range.min < range.max)
                {
                    slider.minValue = range.min;
                    slider.maxValue = range.max;
                }
                else
                {
                    slider.minValue = range.max;
                    slider.maxValue = range.min;
                    slider.direction = Slider.Direction.RightToLeft;
                }
            }

            slider.SetValueWithoutNotify((float)field.GetValue(settings));
            BindMaterialSlider(slider, field);
        }
    }

    private void BindMaterialSlider(Slider slider, FieldInfo field)
    {
        slider.onValueChanged.RemoveAllListeners();

        slider.onValueChanged.AddListener(
            value =>
            {
                var matSettings = CurrentVisuals.GetMaterialSettings(selectedBodyPart);
                object settings = matSettings.type == CreatureVisuals.MaterialOption.Fur ? matSettings.furSettings : matSettings.bsdfSettings;
                field.SetValue(settings, value);
                if (matSettings.type == CreatureVisuals.MaterialOption.Fur)
                {
                    matSettings.furSettings = (CreatureVisuals.FurSettings)settings;
                }
                else
                {
                    matSettings.bsdfSettings = (CreatureVisuals.BSDFSettings)settings;
                }
                CurrentVisuals.SetMaterialSettings(selectedBodyPart, matSettings);
                CreatureVisuals.ApplyMaterials(CurrentBodyMesh, CurrentVisuals);
            });
    }

    private void RebuildShapeTab()
    {
        foreach (Transform child in shapeContentRoot)
            Destroy(child.gameObject);

        object blendShapeStruct = CurrentVisuals.GetBlendShapeStruct(selectedBodyPart);
        var fields = blendShapeStruct.GetType().GetFields();

        foreach (var field in fields)
        {
            if (field.IsLiteral)
                continue;

            if (field.FieldType != typeof(float))
                continue;

            GameObject go = Instantiate(shapeSliderPrefab, shapeContentRoot);

            TMP_Text label = go.GetComponentInChildren<TMP_Text>();

            Slider slider = go.GetComponentInChildren<Slider>();

            label.text = field.Name;

            RangeAttribute range = field.GetCustomAttribute<RangeAttribute>();

            if (range != null)
            {
                slider.minValue = range.min;
                slider.maxValue = range.max;
            }
            else
            {
                slider.minValue = -100f;
                slider.maxValue = 100f;
            }

            slider.SetValueWithoutNotify((float)field.GetValue(blendShapeStruct));

            BindShapeSlider(slider, field);
        }
        Canvas.ForceUpdateCanvases();
        shapeScrollRect.verticalNormalizedPosition = 1f;
    }

    private void BindShapeSlider(Slider slider, FieldInfo field)
    {
        slider.onValueChanged.RemoveAllListeners();

        slider.onValueChanged.AddListener(value =>
        {
            object blendShapes = CurrentVisuals.GetBlendShapeStruct(selectedBodyPart);
            field.SetValue(blendShapes, value);
            CurrentVisuals.SetBlendShapeStruct(selectedBodyPart, blendShapes);
            CreatureVisuals.ApplyBlendShapeWeights(CurrentBodyMesh, CurrentVisuals);
        });
    }

    private void SetBlendShapeStruct(BodyPart selectedBodyPart, object blendShapes)
    {
        throw new NotImplementedException();
    }
}