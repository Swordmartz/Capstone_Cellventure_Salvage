using UnityEngine;

/// <summary>
/// Defines a list of waypoints for a PatrollingDraggable to follow.
/// Create an empty GameObject, attach this, then add child GameObjects as waypoints.
/// Or manually assign waypoints in the inspector.
/// </summary>
public class WaypointPath : MonoBehaviour
{
    [Tooltip("Assign waypoint Transforms in order. If empty, uses child GameObjects automatically.")]
    [SerializeField] private Transform[] waypoints;

    [Tooltip("Loop back to start when reaching the end.")]
    [SerializeField] private bool loop = false;

    private void Awake()
    {
        // Auto-populate from children if not manually assigned
        if (waypoints == null || waypoints.Length == 0)
        {
            waypoints = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                waypoints[i] = transform.GetChild(i);
        }
    }

    public int WaypointCount => waypoints.Length;
    public bool Loop => loop;

    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Length == 0) return null;
        return waypoints[Mathf.Clamp(index, 0, waypoints.Length - 1)];
    }

    public Vector3 GetPosition(int index)
    {
        Transform wp = GetWaypoint(index);
        return wp != null ? wp.position : Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        Transform[] pts = waypoints;

        if (pts == null || pts.Length == 0)
        {
            pts = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                pts[i] = transform.GetChild(i);
        }

        if (pts.Length == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < pts.Length; i++)
        {
            if (pts[i] == null) continue;

            Gizmos.DrawSphere(pts[i].position, 0.15f);

            int next = i + 1;
            if (next < pts.Length && pts[next] != null)
                Gizmos.DrawLine(pts[i].position, pts[next].position);
            else if (loop && pts[0] != null)
                Gizmos.DrawLine(pts[i].position, pts[0].position);
        }
    }
}