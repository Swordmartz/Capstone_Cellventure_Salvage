using UnityEngine;
using UnityEngine.AI;

// Requires a baked NavMesh in the scene, and a NavMeshAgent component
// on the same GameObject as this script.
[RequireComponent(typeof(NavMeshAgent))]
public class pneumonococcalFSM : MonoBehaviour
{
    private enum State
    {
        Wandering,
        MovingToTarget,
        Staying
    }

    [Header("State")]
    [SerializeField] private State currentState = State.Wandering;

    [Header("Wander Settings")]
    public float wanderRadius = 10f;      // how far from current position it can pick a new point
    public float wanderInterval = 3f;     // how often it picks a new random point while wandering

    [Header("Detection Settings")]
    public float detectionRadius = 8f;
    public LayerMask detectionLayer;      // set this to the "EC" layer in the Inspector
    public float detectionCheckInterval = 0.25f; // how often it scans for targets (perf-friendly)

    [Header("Move To Target Settings")]
    public float stopDistance = 1f;       // how close it needs to get before switching to Staying

    [Header("Stay Settings")]
    public float stayDuration = 5f;       // how long it stays at the target before resuming
    public bool stayForever = false;      // if true, ignores stayDuration and stays indefinitely

    [Header("Clone Settings")]
    public int minClones = 1;             // minimum number of clones spawned after staying
    public int maxClones = 3;             // maximum number of clones spawned after staying (inclusive)
    public float cloneSpawnRadius = 2f;   // how far from this object the clones can appear

    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Animation")]
    [Tooltip("Optional. If assigned, the Animator's Dead bool is set to true the moment this enemy dies. " +
             "If left empty, the script will try GetComponent<Animator>() at Awake.")]
    [SerializeField] private Animator animator;

    [Header("Marking")]
    [Tooltip("Whether this enemy has been marked as a valid target (e.g. by a projectile hit or the player's mark ability). " +
             "Mirrors DetectionFSM/InfluenzaFSM's isMarked so systems like SuperMove can treat all enemy types the same way.")]
    public bool isMarked;

    [Header("Star Reporting")]
    [Tooltip("Optional. If assigned, this enemy's death reports one kill to ValuesForStar.ReportEnemyKilled(), " +
             "which increments the WBC (EnemyKilled) count used by the star-rating formula. Leave empty if this " +
             "enemy type shouldn't count toward the WBC score.")]
    [SerializeField] private ValuesForStar valuesForStar;

    private NavMeshAgent agent;
    private Transform detectedTarget;
    private bool isDead = false;

    private float wanderTimer;
    private float detectionTimer;
    private float stayTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Start()
    {
        SetNewWanderDestination();
    }

    // Called by EnemySpawner.Spawn() whenever this object is reused from the pool,
    // so it behaves like a fresh enemy instead of keeping old dead/staying state.
    public void ResetForReuse()
    {
        isDead = false;
        currentHealth = maxHealth;
        detectedTarget = null;
        currentState = State.Wandering;
        wanderTimer = 0f;
        detectionTimer = 0f;
        stayTimer = 0f;

        if (animator != null)
            animator.SetBool("Dead", false);

        // A reused/cloned enemy should never inherit a stale mark from
        // whatever it was cloned/pooled from.
        isMarked = false;

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.Warp(transform.position); // sync the NavMeshAgent to its new spawn position
        }

        SetNewWanderDestination();
    }

    void Update()
    {
        if (isDead) return;

        // Always scan for a target, regardless of state, unless already staying at one
        if (currentState != State.Staying)
        {
            detectionTimer -= Time.deltaTime;
            if (detectionTimer <= 0f)
            {
                detectionTimer = detectionCheckInterval;
                TryDetectTarget();
            }
        }

        switch (currentState)
        {
            case State.Wandering:
                HandleWandering();
                break;

            case State.MovingToTarget:
                HandleMovingToTarget();
                break;

            case State.Staying:
                HandleStaying();
                break;
        }
    }

    // ------------------ State: Wandering ------------------

    private void HandleWandering()
    {
        wanderTimer -= Time.deltaTime;

        bool reachedDestination = !agent.pathPending &&
                                   agent.remainingDistance <= agent.stoppingDistance;

        if (wanderTimer <= 0f || reachedDestination)
        {
            SetNewWanderDestination();
        }
    }

    private void SetNewWanderDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        wanderTimer = wanderInterval;
    }

    // ------------------ State: MovingToTarget ------------------

    private void HandleMovingToTarget()
    {
        if (detectedTarget == null)
        {
            // Target disappeared/destroyed — go back to wandering
            currentState = State.Wandering;
            SetNewWanderDestination();
            return;
        }

        // If the target filled up while we were still walking to it, abandon it
        IsDamage targetDamageScript = detectedTarget.GetComponent<IsDamage>();
        if (targetDamageScript != null && targetDamageScript.currentCount >= targetDamageScript.maxCount)
        {
            detectedTarget = null;
            currentState = State.Wandering;
            SetNewWanderDestination();
            return;
        }

        agent.SetDestination(detectedTarget.position);

        bool reachedTarget = !agent.pathPending &&
                              agent.remainingDistance <= stopDistance;

        if (reachedTarget)
        {
            currentState = State.Staying;
            agent.ResetPath(); // stop moving
            stayTimer = stayDuration;

            // Notify the target that another enemy has started staying at it
            if (targetDamageScript != null)
            {
                targetDamageScript.IncreaseCount();
            }
        }
    }

    // ------------------ State: Staying ------------------

    private void HandleStaying()
    {
        if (detectedTarget == null)
        {
            currentState = State.Wandering;
            SetNewWanderDestination();
            return;
        }

        if (!stayForever)
        {
            stayTimer -= Time.deltaTime;

            if (stayTimer <= 0f)
            {
                LeaveStayTarget();
                CloneAndDisable();
            }
        }
    }

    // Decreases the current target's IsDamage count and clears our reference to it
    private void LeaveStayTarget()
    {
        if (detectedTarget != null)
        {
            IsDamage damageScript = detectedTarget.GetComponent<IsDamage>();
            if (damageScript != null)
            {
                damageScript.DecreaseCount();
            }
        }

        detectedTarget = null;
    }

    void OnDestroy()
    {
        if (currentState == State.Staying)
        {
            LeaveStayTarget();
        }
    }

    // ------------------ Cloning ------------------

    private void CloneAndDisable()
    {
        int cloneCount = Random.Range(minClones, maxClones + 1); // maxClones inclusive

        for (int i = 0; i < cloneCount; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * cloneSpawnRadius;
            randomOffset.y = 0f;
            Vector3 spawnPos = transform.position + randomOffset;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, cloneSpawnRadius, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.Spawn(spawnPos, transform.rotation);
            }
            else
            {
                // Fallback if no pool exists in the scene
                Instantiate(gameObject, spawnPos, transform.rotation);
            }
        }

        ReturnToPool();
    }

    // Sends this enemy back to the pool (or just disables it if no pool exists)
    private void ReturnToPool()
    {
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.Return(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // ------------------ Detection ------------------

    private void TryDetectTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayer);

        Transform closest = null;
        float closestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            // Skip targets that are already full
            IsDamage damageScript = hit.GetComponent<IsDamage>();
            if (damageScript != null && damageScript.currentCount >= damageScript.maxCount)
                continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hit.transform;
            }
        }

        if (closest != null)
        {
            detectedTarget = closest;
            currentState = State.MovingToTarget;
        }
    }

    // ------------------ Health ------------------

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    // Death now leaves the enemy exactly where it died, doing nothing else -
    // it no longer gets destroyed or removed from the scene. The agent is
    // stopped in place (path cleared, movement halted) so it doesn't keep
    // sliding/animating toward wherever it was last headed, and Update()
    // bails out early via the isDead check at the top so no FSM logic
    // (wander/move-to-target/stay/etc.) runs anymore. Mirrors InfluenzaFSM's
    // Die().
    private void Die()
    {
        isDead = true;

        if (animator != null)
            animator.SetBool("Dead", true);

        if (currentState == State.Staying)
        {
            LeaveStayTarget();
        }

        if (agent != null)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        // Report this kill to the star-rating tracker (WBC / EnemyKilled).
        // Guarded so enemies without a valuesForStar assigned don't throw.
        // Only ever reached via TakeDamage() once currentHealth <= 0, so this
        // fires exactly once per enemy, same as InfluenzaFSM.
        if (valuesForStar != null)
        {
            valuesForStar.ReportEnemyKilled();
        }

        // Add extra death behavior here — play an animation, disable colliders,
        // spawn loot, etc.
        Debug.Log(gameObject.name + " has died.");
    }

    // ------------------ Marking ------------------

    // Whether this enemy is dead. Now that Die() leaves the corpse in place
    // instead of destroying it, external systems (e.g. an "eat the dead"
    // ability) can check this the same way they already check
    // DetectionFSM.currentState/InfluenzaFSM.IsDead.
    public bool IsDead => isDead;

    // Marks/unmarks this enemy, mirroring DetectionFSM.SetMarked/isMarked and
    // InfluenzaFSM.SetMarked so targeting systems (e.g. SuperMove, projectiles)
    // can treat all enemy types the same way.
    public void SetMarked(bool value)
    {
        isMarked = value;
    }

    // Clears this enemy's mark. Mirrors DetectionFSM/InfluenzaFSM's ClearMark()
    // so callers don't need to special-case which enemy type they're clearing.
    public void ClearMark()
    {
        isMarked = false;
    }

    // ------------------ Debug Visualization ------------------

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
}