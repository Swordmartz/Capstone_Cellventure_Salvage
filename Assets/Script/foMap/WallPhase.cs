using UnityEngine;

/// <summary>
/// Detects walls within a given range around the player.
/// When a wall is detected, the player's Collider.isTrigger is set to TRUE.
/// When no wall is detected, it's set back to FALSE.
///
/// NOTE: Unity's "IsTrigger" property lives on the Collider component,
/// not on the Rigidbody itself. Rigidbody has no isTrigger field.
/// This script toggles the Collider attached to the same GameObject as the Rigidbody.
///
/// SETUP:
/// 1. Attach this script to your Player GameObject (the one with the Rigidbody + Collider).
/// 2. Assign the "Wall Layer" field to whatever layer your walls are on.
/// 3. Adjust "Detection Range" to taste.
/// 4. If your Collider isn't on the same GameObject, drag it into "Player Collider".
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WallDetectionTrigger : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Radius around the player used to check for walls.")]
    [SerializeField] private float detectionRange = 2f;

    [Tooltip("Only objects on this layer will count as 'walls'.")]
    [SerializeField] private LayerMask wallLayer;

    [Tooltip("Optional: origin point for detection. If left empty, uses this transform's position.")]
    [SerializeField] private Transform detectionOrigin;

    [Header("References")]
    [Tooltip("The collider whose IsTrigger will be toggled. Auto-filled from this GameObject if left empty.")]
    [SerializeField] private Collider playerCollider;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private bool logStateChanges = false;

    [Header("Phasing State (read-only, view in Inspector)")]
    [Tooltip("True while the player is phasing (wall detected / collider is trigger). Ticks and unticks automatically.")]
    [SerializeField] private bool isPhasing = false;

    /// <summary>
    /// Public accessor so other scripts (animation, VFX, movement, etc.) can react to phasing state.
    /// </summary>
    public bool IsPhasing => isPhasing;

    private Rigidbody rb;
    private bool wallCurrentlyDetected = false;

    // Reused buffer to avoid garbage allocation every frame
    private readonly Collider[] overlapResults = new Collider[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider>();
        }

        if (detectionOrigin == null)
        {
            detectionOrigin = transform;
        }

        if (playerCollider == null)
        {
            Debug.LogWarning($"[{nameof(WallDetectionTrigger)}] No Collider found on {gameObject.name}. " +
                              "Assign one in the inspector or add a Collider component.");
        }
    }

    private void Update()
    {
        bool wallDetected = DetectWall();

        // Only update/log when the state actually changes
        if (wallDetected != wallCurrentlyDetected)
        {
            wallCurrentlyDetected = wallDetected;
            ApplyTriggerState(wallCurrentlyDetected);
        }
    }

    /// <summary>
    /// Checks whether any collider on the wall layer is within detectionRange.
    /// </summary>
    private bool DetectWall()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            detectionOrigin.position,
            detectionRange,
            overlapResults,
            wallLayer,
            QueryTriggerInteraction.Ignore
        );

        return hitCount > 0;
    }

    /// <summary>
    /// Applies the isTrigger state to the player's collider.
    /// </summary>
    private void ApplyTriggerState(bool isWallDetected)
    {
        // Phasing bool always tracks wall detection, even if there's no collider assigned.
        isPhasing = isWallDetected;

        if (playerCollider == null) return;

        playerCollider.isTrigger = isWallDetected;

        if (logStateChanges)
        {
            Debug.Log($"[{nameof(WallDetectionTrigger)}] Wall detected: {isWallDetected} -> " +
                      $"Collider.isTrigger set to {playerCollider.isTrigger}, IsPhasing set to {isPhasing}");
        }
    }

    // Visualize the detection range in the editor
    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;

        Vector3 origin = detectionOrigin != null ? detectionOrigin.position : transform.position;
        Gizmos.color = wallCurrentlyDetected ? Color.red : Color.green;
        Gizmos.DrawWireSphere(origin, detectionRange);
    }
}