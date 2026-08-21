using UnityEngine;

/// <summary>
/// Attach this to the player. It owns a timed movement-speed multiplier that
/// pickups (like Collectible) can apply via ApplySpeedBuff(). Your existing
/// movement script doesn't need to change much - just multiply its base
/// speed by CurrentSpeedMultiplier each frame, e.g.:
///
///     float speed = baseMoveSpeed * speedBuffHandler.CurrentSpeedMultiplier;
///
/// This script only tracks and counts down the multiplier - it does not
/// move the player itself.
/// </summary>
public class PlayerSpeedBuffHandler : MonoBehaviour
{
    private enum StackMode
    {
        Overwrite,   // New buff replaces whatever's active (multiplier + duration reset)
        TakeStronger // New buff only applies if its multiplier is bigger than the current one
    }

    [Tooltip("How a new incoming buff interacts with one that's already active. Overwrite: the " +
             "new buff always replaces the old one. TakeStronger: only replaces it if the new " +
             "multiplier is bigger - so a weak pickup can't cut a strong buff's duration short.")]
    [SerializeField] private StackMode stackMode = StackMode.Overwrite;

    /// <summary>Current movement speed multiplier. 1 = no buff active.</summary>
    public float CurrentSpeedMultiplier { get; private set; } = 1f;

    /// <summary>True while a speed buff is currently active.</summary>
    public bool IsBuffActive => remainingDuration > 0f;

    private float remainingDuration;

    /// <summary>
    /// Applies a movement speed buff. multiplier is applied directly to
    /// CurrentSpeedMultiplier (e.g. 1.5 = 50% faster). duration is in
    /// seconds. Behavior when a buff is already active depends on stackMode.
    /// </summary>
    public void ApplySpeedBuff(float multiplier, float duration)
    {
        if (duration <= 0f)
        {
            Debug.LogWarning($"[PlayerSpeedBuffHandler] {name}: ApplySpeedBuff called with a " +
                $"non-positive duration ({duration}) - ignoring.");
            return;
        }

        bool shouldApply = stackMode == StackMode.Overwrite
            || !IsBuffActive
            || multiplier > CurrentSpeedMultiplier;

        if (!shouldApply)
        {
            Debug.Log($"[PlayerSpeedBuffHandler] {name}: incoming buff ({multiplier}x) was weaker than " +
                $"the active one ({CurrentSpeedMultiplier}x) - kept the existing buff (stackMode={stackMode}).");
            return;
        }

        CurrentSpeedMultiplier = multiplier;
        remainingDuration = duration;
    }

    /// <summary>Immediately clears any active buff, returning the multiplier to 1.</summary>
    public void ClearBuff()
    {
        CurrentSpeedMultiplier = 1f;
        remainingDuration = 0f;
    }

    private void Update()
    {
        if (remainingDuration <= 0f)
            return;

        remainingDuration -= Time.deltaTime;
        if (remainingDuration <= 0f)
        {
            remainingDuration = 0f;
            CurrentSpeedMultiplier = 1f;
        }
    }
}