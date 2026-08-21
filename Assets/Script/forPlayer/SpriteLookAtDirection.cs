using UnityEngine;

// Faces this object toward a fixed direction rather than looking at a
// target Transform. Runs in LateUpdate so it always has the final say on
// rotation each frame, overriding anything set earlier (e.g. by
// EnemySplineFollower's alignToDirection, which should stay OFF when this
// script is on the same object).
public class BillboardToDirection : MonoBehaviour
{
    [Header("Direction to face")]
    public Vector3 direction = Vector3.forward; // world-space direction

    [Header("Options")]
    [Tooltip("If true, 'direction' is treated as relative to this object's parent instead of world space.")]
    public bool useLocalSpace = false;
    public bool lockYAxis = false;   // flatten to horizontal-only rotation, keeps it upright
    public bool flip180 = false;     // flip if the object appears backwards

    void LateUpdate()
    {
        Vector3 dir = direction;

        if (useLocalSpace && transform.parent != null)
            dir = transform.parent.TransformDirection(dir);

        if (lockYAxis)
            dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return; // avoid errors on a zero direction

        Quaternion lookRotation = Quaternion.LookRotation(dir.normalized);

        if (flip180)
            lookRotation *= Quaternion.Euler(0f, 180f, 0f);

        transform.rotation = lookRotation;
    }

    // ---------------------------------------------------------------
    // Manual rotation controls — these are the ONLY correct way to change
    // this object's facing while this script is active. Anything that tries
    // to set transform.rotation / transform.Rotate() directly will get
    // overwritten next LateUpdate, since this script recomputes rotation
    // from `direction` every frame regardless of what the Transform holds.
    // ---------------------------------------------------------------

    /// <summary>
    /// Rotates the facing direction horizontally (around world/parent Y) by
    /// the given degrees. Positive = clockwise when viewed from above.
    /// Call this every frame with Time.deltaTime * speed for a continuous
    /// spin, or call it once with a fixed value for a discrete turn.
    /// </summary>
    public void RotateHorizontal(float degrees)
    {
        direction = Quaternion.AngleAxis(degrees, Vector3.up) * direction;
    }

    /// <summary>
    /// Directly sets the facing direction instead of nudging it incrementally.
    /// </summary>
    public void SetDirection(Vector3 newDirection)
    {
        direction = newDirection;
    }

    /// <summary>
    /// Sets the horizontal facing to an absolute compass angle (degrees
    /// around Y, 0 = +Z/world-forward), preserving the current vertical (Y)
    /// component of direction so lockYAxis == false setups don't get
    /// unintentionally flattened by calling this.
    /// </summary>
    public void SetHorizontalAngle(float degreesAroundY)
    {
        float y = direction.y;
        Vector3 flatDir = Quaternion.Euler(0f, degreesAroundY, 0f) * Vector3.forward;
        direction = new Vector3(flatDir.x, y, flatDir.z);
    }

    /// <summary>
    /// Returns the current horizontal facing as a compass angle in degrees
    /// around Y (0 = +Z/world-forward), useful if you need to read back the
    /// current rotation before nudging it further.
    /// </summary>
    public float GetHorizontalAngle()
    {
        Vector3 flatDir = new Vector3(direction.x, 0f, direction.z);
        if (flatDir.sqrMagnitude < 0.0001f) return 0f;
        return Quaternion.LookRotation(flatDir.normalized).eulerAngles.y;
    }
}