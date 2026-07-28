using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Infection meter. Unlike InflammationManager (which tracks
/// currently-active sources), Infection is event-driven: it goes up each time
/// the macrophage eats an enemy or infected cell, and drains on its own once
/// the player hasn't eaten anything for a short grace period.
/// </summary>
public class InfectionManager : MonoBehaviour
{
    public static InfectionManager Instance { get; private set; }

    public enum FoodType { Enemy, InfectedCell }

    [Header("UI")]
    [SerializeField] private Slider infectionSlider;

    [Header("Grey-Out Visual")]
    [Tooltip("Sprite that greys out as a warning once infection gets high but isn't maxed yet (e.g. the macrophage/player sprite).")]
    [SerializeField] private SpriteRenderer targetSprite;
    [Tooltip("Normalized infection (0-1) at which the sprite starts greying out.")]
    [SerializeField, Range(0f, 1f)] private float greyOutThreshold = 0.7f;
    [Tooltip("Color the sprite fades toward at full grey-out (right before max).")]
    [SerializeField] private Color greyedOutColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [Tooltip("How quickly the sprite's color eases toward its target (0-1 scale, per second).")]
    [SerializeField, Min(0.01f)] private float greyOutSmoothSpeed = 3f;

    [Header("Meter Settings")]
    [SerializeField, Min(0.01f)] private float maxInfection = 100f;

    [Header("Eating")]
    [Tooltip("Infection added per enemy eaten.")]
    [SerializeField, Min(0f)] private float infectionPerEnemyEaten = 8f;
    [Tooltip("Infection added per infected cell eaten (typically worse than a plain enemy).")]
    [SerializeField, Min(0f)] private float infectionPerInfectedCellEaten = 15f;

    [Header("Enemy Contact")]
    [Tooltip("Infection added when an enemy reaches/touches the player.")]
    [SerializeField, Min(0f)] private float infectionPerEnemyContact = 5f;

    [Header("Passive Decay")]
    [Tooltip("Seconds with no eating before the meter starts draining on its own.")]
    [SerializeField, Min(0f)] private float idleTimeBeforeDecay = 3f;
    [Tooltip("How much infection drains per second once idle decay kicks in.")]
    [SerializeField, Min(0f)] private float decayPerSecond = 5f;

    [Header("Slider Smoothing")]
    [Tooltip("How quickly the visible slider eases toward the true value (0-1 scale, per second).")]
    [SerializeField, Min(0.01f)] private float smoothSpeed = 2f;

    // The "true" value the logic works with, 0..maxInfection.
    private float currentInfection;

    // What the slider actually shows — eased toward currentInfection each frame
    // so eating/decay don't cause visible pops.
    private float displayedNormalized;

    private float timeSinceLastEat;

    private Color originalSpriteColor = Color.white;
    private bool hasCachedSpriteColor;

    public float NormalizedInfection => displayedNormalized;
    public float CurrentInfection => currentInfection;
    public float MaxInfection => maxInfection;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (targetSprite != null)
        {
            originalSpriteColor = targetSprite.color;
            hasCachedSpriteColor = true;
        }

        // Start fully "not eaten in a while" so decay doesn't wait out a
        // leftover idle timer from a previous scene/session.
        timeSinceLastEat = idleTimeBeforeDecay;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        timeSinceLastEat += Time.deltaTime;

        if (timeSinceLastEat >= idleTimeBeforeDecay && currentInfection > 0f)
        {
            currentInfection = Mathf.Max(0f, currentInfection - decayPerSecond * Time.deltaTime);
        }

        float targetNormalized = maxInfection <= 0f
            ? 0f
            : Mathf.Clamp01(currentInfection / maxInfection);

        displayedNormalized = Mathf.MoveTowards(
            displayedNormalized,
            targetNormalized,
            smoothSpeed * Time.deltaTime);

        if (infectionSlider != null)
            infectionSlider.value = displayedNormalized;

        UpdateGreyOutSprite();
    }

    /// <summary>
    /// Eases the target sprite's color toward greyedOutColor once infection
    /// crosses greyOutThreshold, fully grey by the time it hits max. Below
    /// the threshold it eases back to its original color.
    /// </summary>
    private void UpdateGreyOutSprite()
    {
        if (targetSprite == null || !hasCachedSpriteColor) return;

        float greyAmount = displayedNormalized <= greyOutThreshold
            ? 0f
            : Mathf.InverseLerp(greyOutThreshold, 1f, displayedNormalized);

        Color targetColor = Color.Lerp(originalSpriteColor, greyedOutColor, greyAmount);

        targetSprite.color = Color.Lerp(
            targetSprite.color,
            targetColor,
            greyOutSmoothSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Core increase logic shared by every infection source (eating, enemy
    /// contact, etc.) — adds the amount and resets the idle-decay timer.
    /// </summary>
    private void AddInfection(float amount)
    {
        currentInfection = Mathf.Min(maxInfection, currentInfection + Mathf.Max(0f, amount));
        timeSinceLastEat = 0f;
    }

    /// <summary>
    /// Call this from the macrophage the moment it eats something. Resets the
    /// idle timer, so passive decay only kicks back in after another quiet
    /// period.
    /// </summary>
    public void RegisterEaten(FoodType type)
    {
        float amount = type == FoodType.Enemy
            ? infectionPerEnemyEaten
            : infectionPerInfectedCellEaten;

        AddInfection(amount);
    }

    /// <summary>
    /// Overload for a custom amount, e.g. different enemy tiers/sizes that
    /// should count for more or less than the defaults above.
    /// </summary>
    public void RegisterEaten(float customAmount)
    {
        AddInfection(customAmount);
    }

    /// <summary>
    /// Call this when an enemy reaches/touches the player (getting hit by an
    /// enemy is bad — separate from eating). Uses infectionPerEnemyContact.
    /// </summary>
    public void RegisterEnemyReachedPlayer()
    {
        AddInfection(infectionPerEnemyContact);
    }
}