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
}