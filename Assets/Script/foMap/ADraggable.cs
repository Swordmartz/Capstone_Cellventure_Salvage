using UnityEngine;

/// <summary>
/// Attach to a draggable object alongside DraggableObject.
/// The object moves along a WaypointPath automatically in one direction.
/// When dragged, path movement pauses and drag takes priority.
/// When dropped, the object continues forward from its current position
/// toward the next waypoint ahead — no snapping back, no reversing.
/// </summary>
[RequireComponent(typeof(DraggableObject))]
[RequireComponent(typeof(Rigidbody))]
public class PatrollingDraggable : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private WaypointPath path;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Resume Behaviour")]
    [Tooltip("Distance to a waypoint before moving to the next one.")]
    [SerializeField] private float waypointReachDistance = 0.2f;

    [Tooltip("After dropping, how long to wait before resuming path movement.")]
    [SerializeField] private float resumeDelay = 0.5f;

    private DraggableObject draggable;
    private Rigidbody rb;

    private int currentWaypointIndex = 0;
    private bool isDragging = false;
    private float resumeTimer = 0f;
    private bool reachedEnd = false;

    private void Awake()
    {
        draggable = GetComponent<DraggableObject>();
        rb = GetComponent<Rigidbody>();

        if (path == null)
            Debug.LogWarning($"[PatrollingDraggable] {name}: No WaypointPath assigned!");
    }

    private void Start()
    {
        if (path != null && path.WaypointCount > 0)
            currentWaypointIndex = 0;
    }

    private void Update()
    {
        bool currentlyDragging = draggable.IsDragging();

        // Detect drag start
        if (currentlyDragging && !isDragging)
            OnDragStarted();

        // Detect drag end
        if (!currentlyDragging && isDragging)
            OnDragEnded();

        isDragging = currentlyDragging;

        // Pause while dragging
        if (isDragging) return;

        // Stopped at end
        if (reachedEnd) return;

        if (path == null || path.WaypointCount == 0) return;

        // Count down resume delay after drop
        if (resumeTimer > 0f)
        {
            resumeTimer -= Time.deltaTime;
            return;
        }

        MoveAlongPath();
    }

    // -------------------------------------------------------------------------
    // Drag events
    // -------------------------------------------------------------------------

    private void OnDragStarted()
    {
        resumeTimer = 0f;
        reachedEnd = false;
    }

    private void OnDragEnded()
    {
        // Find the next waypoint AHEAD of current position and continue from there
        currentWaypointIndex = GetNextWaypointAhead();
        resumeTimer = resumeDelay;
    }

    // -------------------------------------------------------------------------
    // Movement
    // -------------------------------------------------------------------------

    private void MoveAlongPath()
    {
        if (currentWaypointIndex >= path.WaypointCount)
        {
            if (path.Loop)
            {
                currentWaypointIndex = 0;
            }
            else
            {
                reachedEnd = true;
                return;
            }
        }

        Vector3 target = path.GetPosition(currentWaypointIndex);
        target.z = rb.position.z; // keep Z locked for 2.5D

        float distance = Vector3.Distance(rb.position, target);

        if (distance <= waypointReachDistance)
        {
            // Reached this waypoint — advance to next
            currentWaypointIndex++;

            if (currentWaypointIndex >= path.WaypointCount)
            {
                if (path.Loop)
                    currentWaypointIndex = 0;
                else
                {
                    reachedEnd = true;
                    return;
                }
            }
        }
        else
        {
            Vector3 newPos = Vector3.MoveTowards(rb.position, target, moveSpeed * Time.deltaTime);
            newPos.z = rb.position.z;
            rb.MovePosition(newPos);
        }
    }

    // -------------------------------------------------------------------------
    // Find next waypoint ahead after drop
    // -------------------------------------------------------------------------

    private int GetNextWaypointAhead()
    {
        if (path == null || path.WaypointCount == 0) return 0;

        // Find the nearest waypoint to current position
        int nearest = 0;
        float minDist = Mathf.Infinity;

        for (int i = 0; i < path.WaypointCount; i++)
        {
            float dist = Vector3.Distance(transform.position, path.GetPosition(i));
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }

        // Target the waypoint AFTER the nearest one
        int next = nearest + 1;

        if (next >= path.WaypointCount)
        {
            if (path.Loop) return 0;
            return path.WaypointCount - 1; // stay at last
        }

        return next;
    }

    // -------------------------------------------------------------------------
    // Editor gizmos
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (path == null || path.WaypointCount == 0) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, path.GetPosition(currentWaypointIndex));
    }
}