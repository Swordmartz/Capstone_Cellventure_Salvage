using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A timed boost ability. Its trigger button stays hidden until the mission
/// timer's remaining time drops to availableAtRemainingSeconds, or until you
/// call MakeAvailable() yourself (e.g. from a wave/score trigger). Clicking
/// the button activates a temporary buff:
///   - Movement speed (via PlayerMovementTry.BoostMultiplier)
///   - Melee cooldown, shrunk on whichever attack/eat script(s) you assign
///   - Meter gain, sped up on a SliderTimer (e.g. the Super Eat bar)
/// Everything reverts to its original value automatically once boostDuration
/// elapses.
/// </summary>
public class BoostAbility : MonoBehaviour
{
    [Header("Button / Availability")]
    [Tooltip("The button the player clicks to activate the boost. Hidden until it becomes available.")]
    [SerializeField] private Button boostButton;
    [Tooltip("The mission timer to watch. The button appears once GetCurrentTime() drops to or below availableAtRemainingSeconds.")]
    [SerializeField] private GameTimer gameTimer;
    [Tooltip("Time remaining on the mission clock (seconds) at which the button appears. E.g. 30 = button shows up once 30 seconds are left.")]
    [SerializeField, Min(0f)] private float availableAtRemainingSeconds = 30f;
    [Tooltip("If true, this ability can only ever be used once — the button never reappears after the boost ends, no matter what. Turn this off if you want it reusable (see requireWaitBetweenUses below).")]
    [SerializeField] private bool oneTimeUse = true;
    [Tooltip("Only relevant if oneTimeUse is false: whether the button re-hides and the availability timer restarts after each use, requiring a wait before it can be used again.")]
    [SerializeField] private bool requireWaitBetweenUses = false;

    [Header("Boost Duration")]
    [Tooltip("How long the boost lasts once activated.")]
    [SerializeField, Min(0.1f)] private float boostDuration = 30f;

    [Header("Movement Boost")]
    [SerializeField] private PlayerMovementTry playerMovement;
    [Tooltip("Movement speed multiplier while boosted (1.5 = +50% speed).")]
    [SerializeField, Min(1f)] private float speedMultiplier = 1.5f;

    [Header("Melee Cooldown Boost")]
    [Tooltip("Optional — assign whichever attack/eat scripts should get a shorter cooldown while boosted.")]
    [SerializeField] private MeleeAttack meleeAttack;
    [SerializeField] private MeleeAttack2 meleeAttack2;
    [Tooltip("Cooldown multiplier while boosted (0.5 = twice as fast attacks/eats).")]
    [SerializeField, Range(0.01f, 1f)] private float cooldownMultiplier = 0.5f;

    [Header("Meter Gain Boost")]
    [Tooltip("The SliderTimer meter (e.g. the Super Eat bar) that should fill faster while boosted.")]
    [SerializeField] private SliderTimer meterToBoost;
    [Tooltip("Regen rate multiplier while boosted (2 = fills twice as fast).")]
    [SerializeField, Min(1f)] private float meterGainMultiplier = 2f;

    private bool isBoosting;
    private bool hasBecomeAvailable;
    private bool hasBeenUsed;

    // Cached originals, restored when the boost ends.
    private float originalMeleeCooldown;
    private float originalMeleeCooldown2;
    private float originalMeterRegenRate;

    public bool IsBoosting => isBoosting;
    public bool HasBeenUsed => hasBeenUsed;

    private void Awake()
    {
        if (boostButton != null)
        {
            boostButton.gameObject.SetActive(false);
            boostButton.onClick.AddListener(ActivateBoost);
        }
    }

    private void Update()
    {
        if (hasBecomeAvailable) return;

        if (gameTimer == null)
        {
            Debug.LogWarning("[BoostAbility] No GameTimer assigned — can't check remaining time. Assign one, or call MakeAvailable() manually.", this);
            return;
        }

        if (gameTimer.timerActive && gameTimer.GetCurrentTime() <= availableAtRemainingSeconds)
            MakeAvailable();
    }

    /// <summary>
    /// Reveals the boost button. Called automatically once the mission
    /// timer's remaining time drops to availableAtRemainingSeconds, but you
    /// can also call this directly (e.g. from a score/wave trigger) instead
    /// of relying on the timer.
    /// </summary>
    public void MakeAvailable()
    {
        if (hasBecomeAvailable) return;

        hasBecomeAvailable = true;

        if (boostButton != null)
            boostButton.gameObject.SetActive(true);
    }

    /// <summary>Hooked to the button's OnClick — starts the boost.</summary>
    public void ActivateBoost()
    {
        if (isBoosting) return;
        if (oneTimeUse && hasBeenUsed) return;

        hasBeenUsed = true;
        StartCoroutine(BoostRoutine());
    }

    private IEnumerator BoostRoutine()
    {
        isBoosting = true;

        if (boostButton != null)
            boostButton.gameObject.SetActive(false);

        ApplyBoost();

        yield return new WaitForSeconds(boostDuration);

        RevertBoost();

        isBoosting = false;

        if (boostButton == null)
            yield break;

        if (oneTimeUse)
        {
            // Used up for good — button stays hidden forever.
            yield break;
        }

        if (requireWaitBetweenUses)
        {
            // Re-lock it so it waits for remaining time to hit the threshold again
            // (only relevant if the mission timer somehow resets/extends).
            hasBecomeAvailable = false;
        }
        else
        {
            boostButton.gameObject.SetActive(true);
        }
    }

    private void ApplyBoost()
    {
        if (playerMovement != null)
            playerMovement.BoostMultiplier = speedMultiplier;

        if (meleeAttack != null)
        {
            originalMeleeCooldown = meleeAttack.meleeCooldown;
            meleeAttack.meleeCooldown = originalMeleeCooldown * cooldownMultiplier;
        }

        if (meleeAttack2 != null)
        {
            originalMeleeCooldown2 = meleeAttack2.meleeCooldown;
            meleeAttack2.meleeCooldown = originalMeleeCooldown2 * cooldownMultiplier;
        }

        if (meterToBoost != null)
        {
            originalMeterRegenRate = meterToBoost.regenRate;
            meterToBoost.regenRate = originalMeterRegenRate * meterGainMultiplier;
        }
    }

    private void RevertBoost()
    {
        if (playerMovement != null)
            playerMovement.BoostMultiplier = 1f;

        if (meleeAttack != null)
            meleeAttack.meleeCooldown = originalMeleeCooldown;

        if (meleeAttack2 != null)
            meleeAttack2.meleeCooldown = originalMeleeCooldown2;

        if (meterToBoost != null)
            meterToBoost.regenRate = originalMeterRegenRate;
    }
}