using UnityEngine;
using UnityEngine.Splines;

public class ItemFlee : MonoBehaviour
{
    [Header("Intro Spline Path")]
    [Tooltip("If assigned, the object will travel along this spline first (from t=0 to t=1) before any flee/return logic runs. Leave empty to skip the intro and behave normally from the start.")]
    public SplineContainer introSpline;
    public float splineTravelSpeed = 3f;

    [Header("Trigger toggle during spline travel")]
    [Tooltip("PHYSICAL collider only — the one that should be solid once this item lands (e.g. so it can block the player or sit on the ground). " +
             "isTrigger is set true while traveling the spline, and false once the end is reached. " +
             "Do NOT assign the same collider here that PickupButton's OnTriggerEnter relies on for pickup detection, " +
             "or pickup will stop firing the moment the item lands and this collider becomes solid again. " +
             "Use a separate, dedicated pickup-detection collider (always isTrigger = true) for that.")]
    public Collider physicsCollider;

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

    private bool isOnSpline = false;
    private float splineT = 0f;
    private float splineLength = 0f;

    void OnEnable()
    {
        // Reset flee/return state every time this object is (re)activated.
        isFleeing = false;
        isReturning = false;
        fleeTimer = 0f;
        cooldownTimer = 0f;
        returnDelayTimer = 0f;

        if (introSpline != null)
        {
            // Always restart the path from the very beginning.
            isOnSpline = true;
            splineT = 0f;
            splineLength = introSpline.CalculateLength();
            transform.position = introSpline.EvaluatePosition(0f);

            if (physicsCollider != null)
                physicsCollider.isTrigger = true;
        }
        else
        {
            isOnSpline = false;
        }
    }

    void Start()
    {
        // Only set the resting position here if there's no intro path;
        // otherwise it gets set once the spline finishes (see FinishSplineTravel).
        if (introSpline == null)
        {
            originalPosition = transform.position;
        }
    }

    void Update()
    {
        // While traveling the intro spline, skip all flee/return logic entirely.
        if (isOnSpline)
        {
            TravelSpline();
            return;
        }

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

                return;
            }

            fleeDirection = AvoidWalls(fleeDirection);
            transform.position += fleeDirection * fleeSpeed * Time.deltaTime;
        }
    }

    private void TravelSpline()
    {
        if (introSpline == null || splineLength <= 0f)
        {
            FinishSplineTravel();
            return;
        }

        float distanceThisFrame = splineTravelSpeed * Time.deltaTime;
        splineT += distanceThisFrame / splineLength;

        if (splineT >= 1f)
        {
            splineT = 1f;
            transform.position = introSpline.EvaluatePosition(splineT);
            FinishSplineTravel();
            return;
        }

        transform.position = introSpline.EvaluatePosition(splineT);
    }

    private void FinishSplineTravel()
    {
        isOnSpline = false;

        if (physicsCollider != null)
            physicsCollider.isTrigger = false;

        // The spot where it lands becomes the "home" position
        // that flee/return logic will return it to.
        originalPosition = transform.position;
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