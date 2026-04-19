using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.VisualScripting;

public class DetectionFSM : MonoBehaviour
{
    public enum EnemyState { Idle, Detecting, Hiding, Returning, Dead }
    public EnemyState currentState = EnemyState.Idle;

    private NavMeshAgent agent;
    public Transform player;
    public Transform originalTarget;
    public LayerMask hideableLayer;

    [Header("Detection Settings")]
    public float detectionRadius = 5f;
    public float searchRadius = 50f;

    [Header("Hide Settings")]
    public float hideTime = 3f;
    public float hideCooldown = 5f;

    [Header("Speed Settings")]
    public float normalSpeed = 3.5f;
    public float maxBoostSpeed = 8f;
    public int maxBoosts = 3;
    public float speedDecayRate = 1f;

    private Transform currentTarget;
    private bool canHide = true;
    private int hideCount = 0;

    [Header("Health Settings")]
    public int maxHealth = 10;
    public int currentHealth;
    public bool isInvincible = false;

    [Header("Attacked")]
    public bool isMarked { get; private set; } = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = normalSpeed;

        if (originalTarget != null)
            agent.SetDestination(originalTarget.position);

        currentHealth = maxHealth; // initialize health

    }

    void Update()
    {
        // Always track the currently active player
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            GameObject activePlayer = GameObject.FindGameObjectWithTag("Player");
            if (activePlayer != null)
                player = activePlayer.transform;
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;

            case EnemyState.Detecting:
                HandleDetecting();
                break;

            case EnemyState.Hiding:
                HandleHiding();
                break;

            case EnemyState.Returning:
                HandleReturning();
                break;

            case EnemyState.Dead:
                break;
        }
    }

    private void HandleIdle()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (canHide && distanceToPlayer <= detectionRadius)
        {
            currentState = EnemyState.Detecting;
        }
    }

    private void HandleDetecting()
    {
        // If no target yet, pick one
        if (currentTarget == null)
        {
            currentTarget = FindRandomHideable();
            if (currentTarget != null)
            {
                agent.SetDestination(currentTarget.position);

                float boostFactor = Mathf.Clamp01(1f - (float)hideCount / maxBoosts);
                float boostedSpeed = Mathf.Lerp(normalSpeed, maxBoostSpeed, boostFactor);
                StartCoroutine(ApplyBoostAfterStop(boostedSpeed));

                Debug.Log($"{name} detected player! Moving to hideable: {currentTarget.name}");
            }
        }

        // Transition to Hiding once reached
        if (currentTarget != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = EnemyState.Hiding;
        }
    }

    private void HandleHiding()
    {
        // Start hiding only once
        if (!isInvincible)   // ensures we don’t restart coroutine every frame
        {
            StartCoroutine(HideTimer());
        }
    }


    private void HandleReturning()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentTarget = null;
            currentState = EnemyState.Idle;
        }
    }
    public void TakeDamage(int amount)
    {
        if (isInvincible)
        {
            Debug.Log($"{name} is invincible! Ignoring damage.");
            return;
        }

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{name} took {amount} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Debug.Log($"{name} has died.");
        currentState = EnemyState.Dead;

        currentHealth = 0; // 👈 zero out HP

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;  // 👈 kill any remaining momentum
            agent.ResetPath();              // 👈 clear any active path
        }
    }
    public void ForceKill()
    {
        currentHealth = 0;
        Die(); // calls the existing private Die() method... 
    }

    private IEnumerator HideTimer()
    {
        isInvincible = true;
        canHide = false;
        hideCount++;

        Debug.Log($"{name} is hiding for {hideTime} seconds...");

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
                    Debug.Log($"{name} exiting hideable {currentTarget.name} at {hit.position}");
                }
            }
        }

        // Return to original target
        if (originalTarget != null)
        {
            agent.SetDestination(originalTarget.position);
            currentState = EnemyState.Returning;
        }

        // Reset
        agent.speed = normalSpeed;
        isInvincible = false;

        yield return new WaitForSeconds(hideCooldown);
        canHide = true;
    }

    private Transform FindRandomHideable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, hideableLayer);
        if (hits.Length == 0) return null;

        int randomIndex = Random.Range(0, hits.Length);
        return hits[randomIndex].transform;
    }

    private IEnumerator ApplyBoostAfterStop(float boostedSpeed)
    {
        if (currentState == EnemyState.Dead) yield break; // 👈 don't boost if dead

        agent.speed = boostedSpeed;
        StartCoroutine(GraduallyReduceSpeed());
        yield break;
    }

    private IEnumerator GraduallyReduceSpeed()
    {
        while (agent.speed > normalSpeed && currentState != EnemyState.Hiding)
        {
            agent.speed -= speedDecayRate * Time.deltaTime;
            if (agent.speed < normalSpeed)
                agent.speed = normalSpeed;
            yield return null;
        }
    }
    public void ApplyStop(float duration)
    {
        if (agent == null) return;
        if (currentState == EnemyState.Dead) return; // 👈 don't apply stop if dead

        StartCoroutine(StopCoroutine(duration));
    }

    private IEnumerator StopCoroutine(float duration)
    {
        float originalSpeed = agent.speed;
        agent.isStopped = true;

        yield return new WaitForSeconds(duration);

        if (currentState == EnemyState.Dead) yield break; // 👈 don't resume if died during stop

        agent.isStopped = false;
        agent.speed = normalSpeed;
        Debug.Log($"{name} resumed movement.");
    }
    public void MarkAsHit()
    {
        isMarked = true;
        Debug.Log($"{gameObject.name} has been marked!");
        // Optional: add a visual cue, e.g. change material tint
        // GetComponent<Renderer>().material.color = Color.red;
    }

    public void ClearMark()
    {
        isMarked = false;
    }

    void OnDrawGizmos()
    {
        // Draw detection radius (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw search radius (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        // If current target exists, draw a line to it
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }

}
