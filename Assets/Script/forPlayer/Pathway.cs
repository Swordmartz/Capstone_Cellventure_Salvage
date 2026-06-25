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
    }

    [Header("Path Points")]
    public PathPoint[] pathPoints;

    [Header("Path Settings")]
    public bool loop = true;
    public bool destroyAtEnd = false;

    [Header("Sprite")]
    public SpriteRenderer spriteRenderer;
    public bool flipSpriteBasedOnDirection = true;

    [Header("References")]
    public Inventory passerbyInventory;
    public PasserbyItemPickup passerbyItemPickup;

    private float progress = 0f;
    private Vector3 lastPosition;
    private int lastWaypointIndex = -1;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

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