using UnityEngine;

public class ItemFlee : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 5f;
    public string playerTag = "Player";
    public string passerbyTag = "Passerby";

    [Header("Flee Settings")]
    [Range(0, 100)]
    public float fleeChance = 50f;
    public float fleeSpeed = 4f;
    public float fleeDuration = 2f;
    public float cooldown = 3f;

    [Header("Return Settings")]
    public float returnSpeed = 2f;
    public float returnDelay = 2f;      // How long to wait before returning
    public float returnThreshold = 0.1f; // How close before snapping to origin

    [Header("Wall Avoidance")]
    public float wallDetectionDistance = 1.5f;
    public LayerMask wallLayer;

    private bool isFleeing = false;
    private bool isReturning = false;
    private float fleeTimer = 0f;
    private float cooldownTimer = 0f;
    private float returnDelayTimer = 0f;
    private Vector3 fleeDirection = Vector3.zero;
    private Vector3 originalPosition;

    void Start()
    {
        // Save original position on start
        originalPosition = transform.position;
    }

    void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // Handle returning
        if (isReturning)
        {
            ReturnToOrigin();
            return;
        }

        // Handle return delay after fleeing
        if (!isFleeing && returnDelayTimer > 0f)
        {
            returnDelayTimer -= Time.deltaTime;

            if (returnDelayTimer <= 0f)
            {
                isReturning = true;
                Debug.Log(gameObject.name + " is returning to original position.");
            }
            return;
        }

        // Detection and flee logic
        if (cooldownTimer <= 0f)
        {
            Transform threat = GetNearestThreat();

            if (threat != null && !isFleeing)
            {
                float roll = Random.Range(0f, 100f);
                if (roll <= fleeChance)
                {
                    StartFlee(threat);
                }
                else
                {
                    cooldownTimer = cooldown;
                    Debug.Log(gameObject.name + " decided not to flee.");
                }
            }
        }

        // Handle fleeing movement
        if (isFleeing)
        {
            fleeTimer -= Time.deltaTime;

            if (fleeTimer <= 0f)
            {
                isFleeing = false;
                cooldownTimer = cooldown;
                returnDelayTimer = returnDelay;
                Debug.Log(gameObject.name + " stopped fleeing, waiting to return.");
                return;
            }

            fleeDirection = AvoidWalls(fleeDirection);
            transform.position += fleeDirection * fleeSpeed * Time.deltaTime;
        }
    }

    private void ReturnToOrigin()
    {
        float dist = Vector3.Distance(transform.position, originalPosition);

        if (dist <= returnThreshold)
        {
            // Snap to original position
            transform.position = originalPosition;
            isReturning = false;
            return;
        }

        // Check for walls while returning
        Vector3 returnDirection = (originalPosition - transform.position).normalized;
        returnDirection.y = 0f;

        if (Physics.Raycast(transform.position, returnDirection, wallDetectionDistance, wallLayer))
        {
            // Try to steer around wall
            for (int angle = 15; angle <= 180; angle += 15)
            {
                Vector3 leftDir = Quaternion.Euler(0, -angle, 0) * returnDirection;
                if (!Physics.Raycast(transform.position, leftDir, wallDetectionDistance, wallLayer))
                {
                    transform.position += leftDir * returnSpeed * Time.deltaTime;
                    return;
                }

                Vector3 rightDir = Quaternion.Euler(0, angle, 0) * returnDirection;
                if (!Physics.Raycast(transform.position, rightDir, wallDetectionDistance, wallLayer))
                {
                    transform.position += rightDir * returnSpeed * Time.deltaTime;
                    return;
                }
            }
        }
        else
        {
            transform.position += returnDirection * returnSpeed * Time.deltaTime;
        }
    }

    private void StartFlee(Transform threat)
    {
        isFleeing = true;
        isReturning = false;
        returnDelayTimer = 0f;
        fleeTimer = fleeDuration;

        fleeDirection = (transform.position - threat.position).normalized;
        fleeDirection.y = 0f;

        Debug.Log(gameObject.name + " is fleeing from " + threat.name);
    }

    private Vector3 AvoidWalls(Vector3 direction)
    {
        if (Physics.Raycast(transform.position, direction, wallDetectionDistance, wallLayer))
        {
            for (int angle = 15; angle <= 180; angle += 15)
            {
                Vector3 leftDir = Quaternion.Euler(0, -angle, 0) * direction;
                if (!Physics.Raycast(transform.position, leftDir, wallDetectionDistance, wallLayer))
                    return leftDir;

                Vector3 rightDir = Quaternion.Euler(0, angle, 0) * direction;
                if (!Physics.Raycast(transform.position, rightDir, wallDetectionDistance, wallLayer))
                    return rightDir;
            }

            isFleeing = false;
            returnDelayTimer = returnDelay;
            Debug.Log(gameObject.name + " is blocked by walls!");
            return Vector3.zero;
        }

        return direction;
    }

    private Transform GetNearestThreat()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        Transform nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(playerTag) || hit.CompareTag(passerbyTag))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = hit.transform;
                }
            }
        }

        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, fleeDirection * wallDetectionDistance);

        // Show original position
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(originalPosition, 0.2f);
    }
}