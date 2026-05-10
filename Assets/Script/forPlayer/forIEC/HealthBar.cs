using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A single health/nutrition bar.
/// Place ONE instance in your scene and assign a UI Slider to it.
/// Each FoodType has a corresponding value that adds or subtracts from the bar.
/// </summary>
public class HealthBar : MonoBehaviour
{
    public static HealthBar Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Slider slider;
    [SerializeField] private float maxValue = 100f;
    [SerializeField] private float startValue = 50f;

    [Header("Type Values — positive adds, negative subtracts")]
    [SerializeField] private float carbsValue = 5f;
    [SerializeField] private float proteinValue = 10f;
    [SerializeField] private float fatValue = -5f;
    [SerializeField] private float aminoacidValue = 8f;
    [SerializeField] private float electrolytesValue = 8f;
    [SerializeField] private float vitaminsValue = 8f;
    [SerializeField] private float wasteValue = -15f;

    private float currentValue;

    private void Awake()
    {
        // Singleton so any script can call HealthBar.Instance.ApplyValue(...)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        currentValue = startValue;
        UpdateSlider();
    }

    /// <summary>
    /// Call this when a tagged object is dropped in the zone.
    /// </summary>
    public void ApplyValue(FoodType type)
    {
        float amount = GetValueForType(type);
        currentValue = Mathf.Clamp(currentValue + amount, 0f, maxValue);
        UpdateSlider();

        Debug.Log($"[HealthBar] Applied {amount} for {type} | Current: {currentValue}/{maxValue}");
    }

    public float GetValueForType(FoodType type)
    {
        switch (type)
        {
            case FoodType.Carbs: return carbsValue;
            case FoodType.Protein: return proteinValue;
            case FoodType.Fat: return fatValue;
            case FoodType.AminoAcid: return aminoacidValue;
            case FoodType.Electrolytes: return electrolytesValue;
            case FoodType.Vitamins: return vitaminsValue;
            case FoodType.Waste: return wasteValue;
            default: return 0f;
        }
    }

    private void UpdateSlider()
    {
        if (slider != null)
        {
            slider.maxValue = maxValue;
            slider.value = currentValue;
        }
    }

    public float CurrentValue => currentValue;
    public float MaxValue => maxValue;
}