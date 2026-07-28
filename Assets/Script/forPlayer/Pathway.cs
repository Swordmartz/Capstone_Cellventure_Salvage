using UnityEngine;

public class PasserbyMultiCurvePath : MonoBehaviour
{
    [System.Serializable]
    public class PathPoint
    {
        public Transform point;

        [Header("Speed toward this point")]
        public float speed = 1f;

        [Header("Reset Inventory at this point")]
        public bool resetInventoryHere = false;

        [Header("Infection")]
        [Tooltip("If true, passing through this waypoint rolls a chance (Infection Chance below) of this passerby becoming infected.")]
        public bool isInfected = false;

        [Tooltip("If true, passing through this waypoint immediately cures any active infection (e.g. a wash station/clean zone on the route).")]
        public bool curesInfectionHere = false;
    }

    [Header("Path Points")]
    public PathPoint[] pathPoints;

    [Header("Path Settings")]
    public bool loop = true;
    public bool destroyAtEnd = false;

    [Header("Sprite")]
    public SpriteRenderer spriteRenderer;
    public bool flipSpriteBasedOnDirection = true;

    [Header("Infection")]
    [Tooltip("Chance (0-1) of becoming infected each time an isInfected waypoint is reached. Only rolled if not already infected.")]
    [Range(0f, 1f)] public float infectionChance = 0.3f;
    [Tooltip("Sprite shown while infected. Reverts to the normal sprite only when actually cured (a curesInfectionHere waypoint) or eaten — it does not clear on its own.")]
    public Sprite infectedSprite;

    [Header("References")]
    public Inventory passerbyInventory;
    public PasserbyItemPickup passerbyItemPickup;

    private float progress = 0f;
    private Vector3 lastPosition;
    private int lastWaypointIndex = -1;

    private InfectedCell infectedCell;
    private Sprite originalSprite;

    private void Awake()
    {
        // Reuse the same InfectedCell component the eating/inflammation
        // systems already understand — add one if this prefab doesn't have
        // it yet, so infected passerbys are automatically eatable and count
        // toward inflammation just like any other infected cell.
        infectedCell = GetComponent<InfectedCell>();
        if (infectedCell == null)
            infectedCell = gameObject.AddComponent<InfectedCell>();

        infectedCell.OnInfectionStateChanged += HandleInfectionStateChanged;
    }

    private void OnDestroy()
    {
        if (infectedCell != null)
            infectedCell.OnInfectionStateChanged -= HandleInfectionStateChanged;
    }

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalSprite = spriteRenderer.sprite;

        if (pathPoints != null && pathPoints.Length > 0 && pathPoints[0].point != null)
            transform.position = pathPoints[0].point.position;

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (pathPoints == null || pathPoints.Length < 2)
            return;

        float currentSpeed = GetCurrentSpeed();
        progress += currentSpeed * Time.deltaTime;

        float maxProgress = loop ? pathPoints.Length : pathPoints.Length - 1;

        if (progress >= maxProgress)
        {
            if (loop)
                progress = 0f;
            else
            {
                progress = maxProgress;
                transform.position = GetPoint(progress);

                if (destroyAtEnd)
                    Destroy(gameObject);
                else
                    enabled = false;

                return;
            }
        }

        int currentWaypointIndex = Mathf.FloorToInt(progress);
        if (currentWaypointIndex != lastWaypointIndex)
        {
            lastWaypointIndex = currentWaypointIndex;
            CheckWaypointReset(currentWaypointIndex);
        }

        Vector3 newPosition = GetPoint(progress);
        Vector3 direction = newPosition - lastPosition;

        transform.position = newPosition;

        if (flipSpriteBasedOnDirection && spriteRenderer != null)
        {
            if (direction.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (direction.x < -0.01f)
                spriteRenderer.flipX = true;
        }

        lastPosition = newPosition;
    }

    private void CheckWaypointReset(int index)
    {
        if (index < 0 || index >= pathPoints.Length) return;

        PathPoint wp = pathPoints[index];

        if (wp.isInfected)
            TryTriggerInfection();

        if (wp.curesInfectionHere)
            infectedCell?.Cure();

        if (!wp.resetInventoryHere) return;

        if (passerbyInventory != null)
        {
            passerbyInventory.ClearItem();
            Debug.Log("Inventory cleared!");
        }
        else
        {
            Debug.LogError("passerbyInventory is not assigned!");
        }

        if (passerbyItemPickup != null)
        {
            passerbyItemPickup.ResetPickup();

        }
        else
        {
            Debug.LogError("passerbyItemPickup is not assigned!");
        }
    }

    /// <summary>
    /// Rolls infectionChance and, on success, infects this passerby via its
    /// InfectedCell component. No-ops if already infected — reaching another
    /// isInfected waypoint doesn't stack or restart the timer early.
    /// </summary>
    private void TryTriggerInfection()
    {
        if (infectedCell == null || infectedCell.IsInfected) return;

        if (Random.value <= infectionChance)
            infectedCell.Infect();
    }

    /// <summary>
    /// Swaps the sprite whenever InfectedCell's infection state changes —
    /// whether that came from TryTriggerInfection above, a curesInfectionHere
    /// waypoint, or something external entirely (getting eaten elsewhere).
    /// There's no auto-clear timer: infection persists until something
    /// actually cures or eats it.
    /// </summary>
    private void HandleInfectionStateChanged(bool infected)
    {
        if (spriteRenderer != null && infectedSprite != null)
            spriteRenderer.sprite = infected ? infectedSprite : originalSprite;
    }

    private float GetCurrentSpeed()
    {
        int targetIndex = Mathf.Clamp(Mathf.FloorToInt(progress) + 1, 0, pathPoints.Length - 1);

        if (pathPoints[targetIndex] == null)
            return 1f;

        return pathPoints[targetIndex].speed;
    }

    private Vector3 GetPoint(float t)
    {
        int pointCount = pathPoints.Length;

        int p1 = Mathf.FloorToInt(t);
        float localT = t - p1;

        int p0 = p1 - 1;
        int p2 = p1 + 1;
        int p3 = p1 + 2;

        if (loop)
        {
            p0 = WrapIndex(p0, pointCount);
            p1 = WrapIndex(p1, pointCount);
            p2 = WrapIndex(p2, pointCount);
            p3 = WrapIndex(p3, pointCount);
        }
        else
        {
            p0 = Mathf.Clamp(p0, 0, pointCount - 1);
            p1 = Mathf.Clamp(p1, 0, pointCount - 1);
            p2 = Mathf.Clamp(p2, 0, pointCount - 1);
            p3 = Mathf.Clamp(p3, 0, pointCount - 1);
        }

        return CatmullRom(
            pathPoints[p0].point.position,
            pathPoints[p1].point.position,
            pathPoints[p2].point.position,
            pathPoints[p3].point.position,
            localT
        );
    }

    private int WrapIndex(int index, int count)
    {
        if (index < 0)
            return count + index % count;

        return index % count;
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private void OnDrawGizmos()
    {
        if (pathPoints == null || pathPoints.Length < 2)
            return;

        int previewSteps = pathPoints.Length * 20;
        float maxProgress = loop ? pathPoints.Length : pathPoints.Length - 1;

        Vector3 previousPoint = GetPoint(0f);

        for (int i = 1; i <= previewSteps; i++)
        {
            float t = maxProgress * i / previewSteps;
            Vector3 point = GetPoint(t);

            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }
    }
}