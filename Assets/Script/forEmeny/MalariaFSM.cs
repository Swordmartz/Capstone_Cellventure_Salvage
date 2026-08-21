using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple state enemy FSM built on NavMeshAgent:
///
///   Roam  -> wanders randomly within roamRadius of its spawn point, pausing
///            at each destination before picking a new one.
///   Chase -> once a Player/RBC is detected within detectionRadius (via
///            Physics.OverlapSphere on the given layer + tag), the agent
///            switches to chasing that target's position every frame and
///            does NOT give up once it starts chasing.
///   Dead  -> HP has hit 0. All movement, detection and infection logic is
///            frozen; the agent stops moving and nothing else runs until
///            the object is reset (e.g. pulled back into the pool).
///
/// Detection runs on its own timer (independent of the current state) so it
/// keeps checking even while roaming, without doing an OverlapSphere every
/// single frame.
///
/// Requires the enemy's NavMeshAgent to already be placed on a baked NavMesh.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class MalariaFSM : MonoBehaviour
{
    private enum State
    {
        Roam,
        Chase,
        Dead
    }

    [Header("State (read-only, for debugging)")]
    [SerializeField] private State currentState = State.Roam;

    [Header("Health")]
    [Tooltip("Max HP this enemy starts with (also what it's restored to on activation reset, e.g. pooling).")]
    [SerializeField] private int maxHealth = 3;

    [Tooltip("Current HP, visible in the Inspector for debugging.")]
    [SerializeField] private int currentHealth;

    public bool IsDead => currentState == State.Dead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    // Set true when hit by a projectile (see ProjectileBehaviour.OnTriggerEnter),
    // mirroring InfluenzaFSM/pneumonococcalFSM's SetMarked() pattern. Purely a
    // flag for other systems to read (e.g. UI, combo tracking) — doesn't affect
    // this FSM's own behavior.
    private bool isMarked;
    public bool IsMarked => isMarked;

    public void SetMarked(bool value)
    {
        isMarked = value;
    }

    [Header("Roaming")]
    [Tooltip("How far from this enemy's spawn point it will wander.")]
    [SerializeField] private float roamRadius = 10f;

    [Tooltip("How long the enemy waits at each roam destination before picking a new one.")]
    [SerializeField] private float roamWaitTime = 2f;

    [Tooltip("NavMeshAgent speed while roaming.")]
    [SerializeField] private float roamSpeed = 2f;

    [Header("Detection")]
    [Tooltip("Radius (in world units) within which the enemy scans for a target.")]
    [SerializeField] private float detectionRadius = 8f;

    [Tooltip("Only colliders on this layer are considered by the detection OverlapSphere. Set this to the 'Player' layer.")]
    [SerializeField] private LayerMask detectionLayer;

    [Tooltip("Only colliders with this tag are treated as a valid target. Use 'Player' or 'RBC' depending on the target.")]
    [SerializeField] private string requiredTag = "RBC";

    [Tooltip("How often (in seconds) the enemy checks for a target while not already chasing. Lower = more responsive, higher = cheaper.")]
    [SerializeField] private float detectionCheckInterval = 0.2f;

    [Header("Chasing")]
    [Tooltip("NavMeshAgent speed while chasing.")]
    [SerializeField] private float chaseSpeed = 4f;

    [Tooltip("How often (in seconds) the destination is refreshed to the target's current position while chasing.")]
    [SerializeField] private float chaseRepathInterval = 0.15f;

    [Header("Infection")]
    [Tooltip("Distance at which the enemy is considered to have 'reached' the RBC and infects it.")]
    [SerializeField] private float infectionDistance = 1f;

    [Tooltip("If true, this enemy deactivates itself (SetActive(false)) right after infecting its target. Uncheck if you want it to keep chasing/existing after infecting.")]
    [SerializeField] private bool disableOnInfect = true;

    private NavMeshAgent agent;
    private Transform target;

    // Cached RBC component on the current target, so we don't call GetComponent
    // every chase tick. Set once when the target is detected/locked in.
    private RBCSplineSpriteSwitcher targetRBC;

    // True once this enemy has infected its current target, so SetInfected(true)
    // only fires once per target rather than every frame while overlapping.
    private bool hasInfectedTarget;

    private Vector3 roamOrigin;

    private bool isWaitingAtRoamPoint;
    private float roamWaitTimer;

    private float detectionTimer;
    private float chaseRepathTimer;

    // Set whenever this object is (re)activated (fresh spawn OR pulled back
    // out of an object pool). Consumed in Update() once the agent confirms
    // it's actually placed on the NavMesh at its new position.
    //
    // This used to be handled directly inside OnEnable(), gated on
    // `agent.isOnNavMesh`. The problem: right after a pooled object is
    // SetActive(true)'d, the agent doesn't always report isOnNavMesh == true
    // yet on that exact frame (e.g. if the spawner sets position/Warps the
    // agent in a call that runs after SetActive, which is what triggers this
    // OnEnable in the first place). When that check failed, the whole reset
    // block was skipped — so a pooled enemy that had last been disabled
    // mid-Chase (e.g. right after infecting a target, via disableOnInfect)
    // would come back out of the pool still in State.Chase, still pointing
    // at its old (now-infected/stale) target, and would never roam again.
    //
    // Deferring the reset into Update() and retrying every frame until the
    // agent is confirmed on the mesh fixes this without caring about the
    // exact order the spawner sets position vs. calls SetActive.
    private bool pendingActivationReset;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        roamOrigin = transform.position;
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        // Don't touch state here directly (see pendingActivationReset comment
        // above) — just flag that a reset is due, and let Update() apply it
        // once the agent is actually ready.
        pendingActivationReset = true;
    }

    private void Update()
    {
        if (pendingActivationReset)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                target = null;
                targetRBC = null;
                hasInfectedTarget = false;
                isWaitingAtRoamPoint = false;
                isMarked = false;

                // Revive/restore full HP whenever this object is (re)activated,
                // e.g. pulled fresh out of an object pool after a previous death.
                currentHealth = maxHealth;
                agent.isStopped = false;

                // Re-anchor roaming around wherever this enemy actually was
                // (re)placed, not wherever it happened to sit in the pool
                // (e.g. parented under the spawner) or its very first spawn
                // point back when Awake() originally ran.
                roamOrigin = transform.position;

                EnterRoam();
                pendingActivationReset = false;
            }
            else
            {
                // Not placed on the NavMesh yet this frame (agent may still
                // be settling after Warp/enable) — skip all other logic and
                // try again next Update rather than running stale state.
                return;
            }
        }

        // Dead enemies do nothing at all: no detection, no roaming/chasing,
        // no infecting, and they are never auto-disabled or destroyed — they
        // stay in the scene exactly where they died until something external
        // (e.g. a pool manager) reactivates them. This check has to come
        // before everything else below.
        if (currentState == State.Dead)
            return;

        // Detection keeps running independently of state, but once we're
        // chasing there's no need to keep scanning for a new target.
        if (currentState != State.Chase)
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
            case State.Roam:
                UpdateRoam();
                break;
            case State.Chase:
                UpdateChase();
                break;
        }
    }

    // ---------------------------------------------------------------
    // Health / Death
    // ---------------------------------------------------------------

    /// <summary>
    /// Applies damage to this enemy. Once HP reaches 0, it transitions to
    /// State.Dead and Die() takes over — no further calls to this do anything.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (currentState == State.Dead || amount <= 0)
            return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    /// <summary>
    /// Instantly kills this enemy regardless of current HP.
    /// </summary>
    public void Kill()
    {
        if (currentState == State.Dead)
            return;

        currentHealth = 0;
        Die();
    }

    private void Die()
    {
        currentState = State.Dead;

        // Drop whatever target/chase data it had — nothing should reference
        // this enemy as an active threat anymore.
        target = null;
        targetRBC = null;
        hasInfectedTarget = false;
        isWaitingAtRoamPoint = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Deliberately NOT calling SetActive(false) or Destroy() here — the
        // corpse stays exactly where it is, immobile, until something external
        // (e.g. a pool manager or level cleanup) decides to reclaim it.

        // Hook animation/VFX/sound here, e.g.:
        // animator.SetTrigger("Die");
    }

    // ---------------------------------------------------------------
    // Detection
    // ---------------------------------------------------------------

    private void TryDetectTarget()
    {
        // Once a target is locked in, never overwrite it with a different one.
        // (Update() already skips calling this while State.Chase is active, but
        // this guard makes the lock-on explicit and safe even if that changes later.)
        if (target != null)
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            if (!string.IsNullOrEmpty(requiredTag) && !hit.CompareTag(requiredTag))
                continue;

            // Cache the RBC's switcher component (if it has one) so UpdateChase()
            // doesn't need to call GetComponent every tick. GetComponentInParent
            // covers cases where the Collider and RBCSplineSpriteSwitcher live on
            // different levels of the hierarchy.
            RBCSplineSpriteSwitcher rbc = hit.GetComponentInParent<RBCSplineSpriteSwitcher>();

            // Skip RBCs that are already infected — otherwise malaria spawned
            // right next to an infected RBC (e.g. from SpawnMalaria()) would
            // immediately lock back onto it instead of roaming to find a new one.
            if (rbc != null && rbc.IsInfected)
                continue;

            target = hit.transform;
            targetRBC = rbc;
            hasInfectedTarget = false;

            EnterChase();
            return;
        }
    }

    // ---------------------------------------------------------------
    // Roam State
    // ---------------------------------------------------------------

    private void EnterRoam()
    {
        currentState = State.Roam;
        agent.speed = roamSpeed;
        isWaitingAtRoamPoint = false;
        PickNewRoamDestination();
    }

    private void UpdateRoam()
    {
        if (isWaitingAtRoamPoint)
        {
            roamWaitTimer -= Time.deltaTime;
            if (roamWaitTimer <= 0f)
            {
                isWaitingAtRoamPoint = false;
                PickNewRoamDestination();
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            isWaitingAtRoamPoint = true;
            roamWaitTimer = roamWaitTime;
        }
    }

    private void PickNewRoamDestination()
    {
        // Random.insideUnitSphere spreads in all 3 axes, including vertically --
        // on a map with multiple floors close together, that can pick a point
        // above/below the current floor, which NavMesh.SamplePosition then snaps
        // to an odd/wrong nearby surface. Using a horizontal circle instead keeps
        // roam candidates on the same floor the enemy is actually standing on.
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * roamRadius;
        Vector3 randomPoint = roamOrigin + new Vector3(randomCircle.x, 0f, randomCircle.y);

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit navHit, roamRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
    }

    // ---------------------------------------------------------------
    // Chase State
    // ---------------------------------------------------------------

    private void EnterChase()
    {
        currentState = State.Chase;
        agent.speed = chaseSpeed;
        chaseRepathTimer = 0f;
    }

    private void UpdateChase()
    {
        // Target's collider/GameObject was destroyed or deactivated mid-chase.
        // (Per current design the enemy never gives up on its own otherwise.)
        if (target == null)
        {
            EnterRoam();
            return;
        }

        // Target got infected by someone/something else mid-chase (i.e. not by
        // this enemy reaching it) -> abandon the chase and go back to roaming
        // rather than continuing to follow an already-infected RBC.
        if (targetRBC != null && targetRBC.IsInfected && !hasInfectedTarget)
        {
            target = null;
            targetRBC = null;
            EnterRoam();
            return;
        }

        // Reached the target -> infect it (only once per target).
        if (!hasInfectedTarget && targetRBC != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            if (distanceToTarget <= infectionDistance)
            {
                targetRBC.SetInfected(true);
                hasInfectedTarget = true;

                if (disableOnInfect)
                {
                    // Deactivated rather than Destroy()'d, since this project uses
                    // object pooling elsewhere (e.g. the spline enemy pool) — swap
                    // to Destroy(gameObject) instead if this enemy type isn't pooled.
                    gameObject.SetActive(false);
                    return;
                }
            }
        }

        chaseRepathTimer -= Time.deltaTime;
        if (chaseRepathTimer <= 0f)
        {
            chaseRepathTimer = chaseRepathInterval;
            agent.SetDestination(target.position);

            // If the path comes back Partial or Invalid, the elevation change
            // (ramp/ledge) between the enemy and its target likely isn't fully
            // connected in the baked NavMesh. Fix by re-baking with a higher
            // Step Height / Max Slope on the Navigation window's Agent settings,
            // or by placing a NavMeshLink across the gap.
            if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                Debug.LogWarning($"[MalariaFSM] {name}: path to target is {agent.pathStatus} — " +
                    "check NavMesh bake settings (Max Slope / Step Height) around the elevation change.");
            }
        }
    }

    // ---------------------------------------------------------------
    // Gizmos
    // ---------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = currentState == State.Dead ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(Application.isPlaying ? roamOrigin : transform.position, roamRadius);
    }
}