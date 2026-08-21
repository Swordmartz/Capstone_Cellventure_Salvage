using UnityEngine;

/// <summary>
/// Put this on the collectible prefab (the one CollectibleSpawnPoint spawns).
/// Requires a Collider on this object with "Is Trigger" checked. When the
/// player touches it, it applies a temporary movement speed buff via
/// PlayerSpeedBuffHandler (found on the player, or one of its parents) and
/// destroys itself.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    [Tooltip("Only a collider with this tag counts as the player.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Speed Buff")]
    [Tooltip("Movement speed multiplier applied to the player. 1.5 = 50% faster, 2 = double speed.")]
    [SerializeField] private float speedMultiplier = 1.5f;

    [Tooltip("How long (in seconds) the speed buff lasts.")]
    [SerializeField] private float buffDuration = 5f;

    [Header("Feedback (optional)")]
    [Tooltip("Optional particle effect prefab spawned at the pickup point when collected.")]
    [SerializeField] private GameObject pickupEffectPrefab;

    [Tooltip("Optional sound played when collected. Played via AudioSource.PlayClipAtPoint so it " +
             "survives this object being destroyed.")]
    [SerializeField] private AudioClip pickupSfx;

    [Range(0f, 1f)]
    [SerializeField] private float pickupSfxVolume = 1f;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        if (!other.CompareTag(playerTag))
            return;

        // The trigger collider that touched us might be on a child of the
        // player root (e.g. a capsule collider under a rig), so search
        // parents too, not just the exact object that hit the trigger.
        var buffHandler = other.GetComponentInParent<PlayerSpeedBuffHandler>();
        if (buffHandler == null)
        {
            Debug.LogWarning($"[Collectible] {name}: player (tag '{playerTag}') touched this pickup but " +
                "no PlayerSpeedBuffHandler was found on it or its parents - no buff was applied, but " +
                "the pickup is still being collected.");
        }
        else
        {
            buffHandler.ApplySpeedBuff(speedMultiplier, buffDuration);
        }

        collected = true;

        if (pickupEffectPrefab != null)
            Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);

        if (pickupSfx != null)
            AudioSource.PlayClipAtPoint(pickupSfx, transform.position, pickupSfxVolume);

        Destroy(gameObject);
    }
}