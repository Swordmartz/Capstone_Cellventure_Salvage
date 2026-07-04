using UnityEngine;

/// <summary>
/// CylinderFaceCamera
/// ───────────────────
/// Attach to the CYLINDER mesh itself.
/// Rotates it every frame so it always faces the main camera.
/// Keeps the cylinder upright (only rotates around the Y axis) —
/// good for something meant to stand vertically, like a wall/pillar
/// with the reveal shader on it.
/// </summary>
public class CylinderFaceCamera : MonoBehaviour
{
    [Tooltip("If true, only rotates around the Y axis so the cylinder stays upright. " +
             "Turn off for full billboard (faces camera on all axes).")]
    public bool lockYAxisOnly = true;

    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
    }

    void LateUpdate()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null) return;
        }

        Vector3 dir = transform.position - _cam.transform.position;

        if (lockYAxisOnly)
            dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}