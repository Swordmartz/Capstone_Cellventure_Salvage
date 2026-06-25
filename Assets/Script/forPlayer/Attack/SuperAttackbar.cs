using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tracks the super attack meter. Charge is added by combo milestones via
/// AddCharge() — called from ComboCounterUI whenever the combo hits 5, 10, 20, etc.
/// Once full, the button becomes interactable and triggers SuperAttack.PerformSuperAttack().
/// </summary>
public class SuperAttackBarUI : MonoBehaviour
{
    [Header("References")]
    public SuperAttack superAttack;
    public Slider barSlider;
    public Button superButton;

    [Header("Bar Settings")]
    public float maxCharge = 100f;
    [SerializeField] private float currentCharge = 0f;

    [Header("Time-Based Fill")]
    [Tooltip("Charge gained per second, passively, at all times. Set to 0 to disable.")]
    public float chargePerSecond = 0f;

    public float CurrentCharge => currentCharge;
    public bool IsReady => currentCharge >= maxCharge;

    void Start()
    {
        if (barSlider != null)
        {
            barSlider.minValue = 0;
            barSlider.maxValue = maxCharge;
        }

        if (superButton != null)
            superButton.onClick.AddListener(OnSuperButtonClicked);

        RefreshUI();
    }

    void Update()
    {
        if (!IsReady && chargePerSecond > 0f)
            currentCharge = Mathf.Min(currentCharge + chargePerSecond * Time.deltaTime, maxCharge);

        RefreshUI();
    }

    /// <summary>
    /// Called by ComboCounterUI when a combo milestone is reached.
    /// Adds a fixed amount of charge to the super bar.
    /// </summary>
    public void AddCharge(float amount)
    {
        if (IsReady) return;
        currentCharge = Mathf.Min(currentCharge + amount, maxCharge);
        RefreshUI();
    }

    /// <summary>Legacy support — kept so RapidAttackStackUI still compiles if it calls AddCombo().</summary>
    public void AddCombo() => AddCharge(0f);

    private void RefreshUI()
    {
        if (barSlider != null)
            barSlider.value = currentCharge;

        if (superButton != null)
            superButton.interactable = IsReady;
    }

    private void OnSuperButtonClicked()
    {
        if (!IsReady || superAttack == null) return;

        int hitCount = superAttack.PerformSuperAttack();
        Debug.Log($"[SuperAttackBarUI] Super attack used — hit {hitCount} enemy(ies).");

        currentCharge = 0f;
        RefreshUI();
    }
}