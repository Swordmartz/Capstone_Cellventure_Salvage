using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class DetectionTest : MonoBehaviour
{
    [Header("Pathing")]
    private NavMeshAgent agent;

    [Header("Player Detection")]
    public Transform player;
    public float detectionRadius = 5f;

    [Header("Hideable Settings")]
    public LayerMask hideableLayer;
    public float searchRadius = 50f;
    public float hideTime = 3f;       // time to wait at hideable
    public float hideCooldown = 5f;   // cooldown before hiding again

    [Header("Speed Settings")]
    public float normalSpeed = 3.5f;
    public float maxBoostSpeed = 8f;
    public int maxBoosts = 3;
    public float speedDecayRate = 1f;

    private Transform currentTarget;
    public Transform originalTarget;  // assign in Inspector
    private bool isHiding = false;
    private bool canHide = true;
    private int hideCount = 0;

    // Invincibility flag
    public bool isInvincible = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component is missing!");
        }

        agent.speed = normalSpeed;

        if (originalTarget != null)
        {
            agent.SetDestination(originalTarget.position);
        }
    }

    void Update()
    {
        if (player == null || agent == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (!isHiding && canHide && distanceToPlayer <= detectionRadius)
        {
            Transform nearestHideable = FindNearestHideable();

            if (nearestHideable != null)
            {
                if (currentTarget != nearestHideable)
                {
                    currentTarget = nearestHideable;
                    agent.SetDestination(currentTarget.position);

                    float boostFactor = Mathf.Clamp01(1f - (float)hideCount / maxBoosts);
                    float boostedSpeed = Mathf.Lerp(normalSpeed, maxBoostSpeed, boostFactor);

                    agent.speed = boostedSpeed;
                    StartCoroutine(GraduallyReduceSpeed());

                    Debug.Log($"{name} detected player! Moving to hideable: {currentTarget.name} with boosted speed {agent.speed}");
                }
            }
        }

        if (currentTarget != null && !isHiding && canHide)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                StartCoroutine(HideTimer());
            }
        }
    }

    private IEnumerator HideTimer()
    {
        isHiding = true;
        canHide = false;
        hideCount++;

        // Enable invincibility while hiding
        isInvincible = true;
        Debug.Log($"{name} is hiding (invincible) for {hideTime} seconds... (Hide #{hideCount})");

        yield return new WaitForSeconds(hideTime);

        // Exit from random point inside collider
        if (currentTarget != null)
        {
            Collider col = currentTarget.GetComponent<Collider>();
            if (col != null)
            {
                Bounds bounds = col.bounds;
                Vector3 randomPoint = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    bounds.center.y,
                    Random.Range(bounds.min.z, bounds.max.z)
                );

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    Debug.Log($"{name} exiting hideable {currentTarget.name} at random point inside collider: {hit.position}");

                    while (!agent.pathPending && agent.remainingDistance > agent.stoppingDistance)
                    {
                        yield return null;
                    }
                }
            }
        }

        // Return to original target
        if (originalTarget != null)
        {
            agent.SetDestination(originalTarget.position);
            Debug.Log($"{name} finished hiding. Returning to original target: {originalTarget.name}");
        }

        // Reset speed and invincibility
        agent.speed = normalSpeed;
        isInvincible = false;
        Debug.Log($"{name} is no longer invincible.");

        isHiding = false;
        currentTarget = null;

        yield return new WaitForSeconds(hideCooldown);
        canHide = true;
        Debug.Log($"{name} can hide again after cooldown.");
    }

    private IEnumerator GraduallyReduceSpeed()
    {
        while (agent.speed > normalSpeed && !isHiding)
        {
            agent.speed -= speedDecayRate * Time.deltaTime;
            if (agent.speed < normalSpeed)
                agent.speed = normalSpeed;
            yield return null;
        }
    }

    private Transform FindNearestHideable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, hideableLayer);
        if (hits.Length == 0) return null;

        Transform nearest = hits[0].transform;
        float minDist = Vector3.Distance(transform.position, nearest.position);

        foreach (Collider c in hits)
        {
            float dist = Vector3.Distance(transform.position, c.transform.position);
            if (dist < minDist)
            {
                nearest = c.transform;
                minDist = dist;
            }
        }

        return nearest;
    }
}
