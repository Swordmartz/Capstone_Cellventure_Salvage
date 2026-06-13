using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// SplineExplorerPlayer
/// ────────────────────
/// The spline is a DIRECTION GUIDE only.
/// • Joystick Y  → move forward/backward in the spline's direction at that point
/// • Joystick X  → strafe freely left/right
/// • No input    → player stands still completely
/// • Y position  → smoothly follows spline height ONLY when within heightSnapRange
/// • No auto-walk, no leash, no position restriction
///
/// RIGIDBODY SETTINGS
///   Drag: 8  |  Angular Drag: 10  |  Use Gravity: ON
///   Freeze Rotation X: ✓  |  Freeze Rotation Z: ✓
///   Freeze Position Y: OFF  |  Interpolate: Interpolate
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SplineExplorerPlayer : MonoBehaviour
{
    [Header("References")]
    public SplineContainer splineContainer;
    public Joystick joystick;

    [Header("Movement")]
    public float forwardSpeed = 5f;
    public float backwardSpeed = 3f;
    public float sideSpeed = 4f;

    [Header("Rotation")]
    [Range(1f, 20f)]
    public float rotationSmoothing = 8f;

    [Header("Height Tracking")]
    [Tooltip("How smoothly the player's Y follows the spline height. Keep LOW (3-6) to avoid jitter.")]
    [Range(0.1f, 20f)]
    public float heightFollowSpeed = 5f;

    [Tooltip("How close the player must be (XZ distance) to the spline before height snapping activates.")]
    public float heightSnapRange = 5f;

    // ── Private ───────────────────────────────────────────────────────────────
    private Rigidbody _rb;
    private float _splineT;
    private float _splineLength;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;

        if (splineContainer == null)
        {
            Debug.LogError("[SplineExplorer] Assign a SplineContainer!");
            enabled = false;
            return;
        }

        _splineLength = splineContainer.CalculateLength();

        // Initialise _splineT to the closest point just once at start
        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            splineContainer.transform.InverseTransformPoint(transform.position),
            out _, out _splineT);
    }

    void FixedUpdate()
    {
        if (splineContainer == null || joystick == null) return;

        float inputForward = joystick.Vertical;
        float inputSide = joystick.Horizontal;

        // Advance spline T only from joystick — never from player world position
        AdvanceSplineT(inputForward);

        // Move relative to the camera — joystick up = away from camera,
        // joystick right = right of camera. Spline only drives Y height.
        Transform cam = Camera.main != null ? Camera.main.transform : transform;
        Vector3 forward = new Vector3(cam.forward.x, 0f, cam.forward.z).normalized;
        Vector3 right = new Vector3(cam.right.x, 0f, cam.right.z).normalized;

        // Build XZ velocity from joystick — completely free, no clamping
        float speed = inputForward >= 0f ? forwardSpeed : backwardSpeed;
        Vector3 desiredVel = forward * (inputForward * speed)
                           + right * (inputSide * sideSpeed);

        // Apply XZ velocity; preserve Y for gravity/height
        Vector3 vel = _rb.linearVelocity;
        vel.x = desiredVel.x;
        vel.z = desiredVel.z;
        _rb.linearVelocity = vel;

        // Height: only snap to spline Y if within heightSnapRange
        Vector3 splinePos = GetSplinePositionWorld(_splineT);
        float xzDistance = Vector2.Distance(
            new Vector2(_rb.position.x, _rb.position.z),
            new Vector2(splinePos.x, splinePos.z));

        if (xzDistance <= heightSnapRange)
        {
            Vector3 pos = _rb.position;

            // Lerp for smooth gradual follow — avoids fighting physics over time
            pos.y = Mathf.Lerp(pos.y, splinePos.y, heightFollowSpeed * Time.fixedDeltaTime);
            _rb.position = pos;

            // Kill Y velocity so gravity doesn't fight height tracking
            vel = _rb.linearVelocity;
            vel.y = 0f;
            _rb.linearVelocity = vel;
        }

        // Rotate to face movement direction
        if (desiredVel.sqrMagnitude > 0.01f)
        {
            Vector3 flatDir = new Vector3(desiredVel.x, 0f, desiredVel.z).normalized;
            Quaternion targetRot = Quaternion.LookRotation(flatDir, Vector3.up);
            _rb.MoveRotation(Quaternion.Slerp(
                _rb.rotation, targetRot,
                rotationSmoothing * Time.fixedDeltaTime));
        }
    }

    // ── Update spline T via projection ──────────────────────────────────────
    void AdvanceSplineT(float inputForward)
    {
        // Always re-project so T stays in sync with player's actual position.
        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            splineContainer.transform.InverseTransformPoint(transform.position),
            out _, out float projectedT);

        _splineT = Mathf.Clamp01(projectedT);
    }

    // ── Spline Helpers ────────────────────────────────────────────────────────
    Vector3 GetSplinePositionWorld(float t)
    {
        Vector3 local = SplineUtility.EvaluatePosition(splineContainer.Spline, t);
        return splineContainer.transform.TransformPoint(local);
    }

    Vector3 GetSplineTangentWorld(float t)
    {
        Vector3 local = SplineUtility.EvaluateTangent(splineContainer.Spline, t);
        return splineContainer.transform.TransformDirection(local).normalized;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (splineContainer == null || !Application.isPlaying) return;

        Vector3 splinePos = GetSplinePositionWorld(_splineT);

        // Dot on spline showing current guide position
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(splinePos, 0.2f);

        // Line from spline guide point to player
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(splinePos, transform.position);

        // Forward direction arrow
        Gizmos.color = Color.green;
        Gizmos.DrawRay(splinePos, GetSplineTangentWorld(_splineT) * 2f);

        // Draw heightSnapRange as a circle on XZ plane
        Gizmos.color = Color.red;
        DrawCircleGizmo(transform.position, heightSnapRange, 32);
    }

    void DrawCircleGizmo(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(
                Mathf.Cos(angle) * radius, 0f,
                Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
#endif
}