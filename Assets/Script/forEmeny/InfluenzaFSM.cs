using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class InfluenzaFSM : MonoBehaviour
{
    private enum State
    {
        Roam,
        Chase,
        Stay
    }

    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private LayerMask targetLayer;

    [Header("Roam Settings")]
    [SerializeField] private float roamRadius = 30f;
    [SerializeField] private float roamSpeed = 3.5f;
    [SerializeField] private float roamPointWaitTime = 2f;
    [SerializeField] private float arriveDistance = 0.5f;
    [SerializeField] private Vector3 firstMoveDirection = Vector3.back;
    [SerializeField] private float firstMoveDistanceMin = 15f;
    [SerializeField] private float firstMoveDistanceMax = 35f;
    [SerializeField] private bool performFirstMove = true;
    [SerializeField] private bool goingDown;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private bool useLineOfSight = true;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float loseTargetRadius = 15f;
    [SerializeField] private float chaseArriveDistance = 1f;

    [Header("Health Settings")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP;
    [SerializeField] private bool isDead;

    [Header("Animation")]
    [Tooltip("Optional. If assigned, the Animator's Dead bool is set to true the moment this enemy dies. " +
             "If left empty, the script will try GetComponent<Animator>() at Awake.")]
    [SerializeField] private Animator animator;

    [Header("Marking")]
    [Tooltip("Whether this enemy has been marked as a valid target (e.g. by the player's mark ability). " +
             "Mirrors DetectionFSM's isMarked so systems like SuperMove can treat both enemy types the same way.")]
    public bool isMarked;

    [Header("Star Reporting")]
    [Tooltip("Optional. If assigned, this enemy's death reports one kill to ValuesForStar.ReportEnemyKilled(), " +
             "which increments the WBC (EnemyKilled) count used by the star-rating formula. Leave empty if this " +
             "enemy type shouldn't count toward the WBC score (e.g. a non-combat spawn).")]
    [SerializeField] private ValuesForStar valuesForStar;

    [Header("Clone Settings")]
    [SerializeField] private int minCloneCount = 1;
    [SerializeField] private int maxCloneCount = 3;
    [SerializeField] private float cloneSpawnRadius = 3f;
    [SerializeField] private float cloneSpawnDelay = 0f;

    [Header("Obstacle Interaction")]
    [Tooltip("How close (distance) the agent needs to get to a reroute destination for it to count as 'arrived', both for SolidObstacle's own away-from-wall push and for the randomized point picked afterward.")]
    [SerializeField] private float rerouteArriveDistance = 0.5f;

    [Tooltip("Safety timeout (seconds) for each reroute phase in case the destination is never actually reachable, or no valid randomized point could be found in time. Without this, a bad point could stall the FSM in the reroute/detour phase forever instead of falling back to normal behaviour.")]
    [SerializeField] private float rerouteMaxWaitTime = 3f;

    [Tooltip("Base distance for the randomized detour point picked after the enemy has fully arrived at SolidObstacle's away-from-wall destination. This is meant to send the enemy somewhere genuinely different, not just nudge it a step sideways - increase this if enemies still feel like they're lingering right next to the wall after rerouting.")]
    [SerializeField] private float rerouteRandomizeRadius = 8f;

    [Tooltip("Half-angle (degrees) of the cone, centered on the direction SolidObstacle just pushed the enemy away from the wall, within which the randomized detour point is chosen. This is what actually keeps the enemy moving further away from the wall instead of picking a fully random point that could easily land back toward the wall or the target. Lower = stays closer to a straight continuation of the away-from-wall push; higher = more spread, but risks curving back toward the wall.")]
    [SerializeField] private float rerouteAwayBiasAngle = 50f;

    [Tooltip("How many times to retry sampling a randomized detour point at the SAME fixed distance (rerouteRandomizeRadius) before falling back to a wider search. A single sample right next to a wall can land off the NavMesh and silently fail - this retries a few times at the same distance so successful detours stay a consistent length.")]
    [SerializeField] private int rerouteDetourSampleAttempts = 4;

    [Tooltip("Only used if every fixed-distance attempt above fails (e.g. a genuinely tight corner). Retries a few more times at a progressively larger distance so the enemy still gets a detour instead of getting stuck - this is the only situation where the detour distance should come out longer than usual.")]
    [SerializeField] private int rerouteDetourFallbackAttempts = 2;

    private NavMeshAgent _agent;
    private State _currentState;
    private Vector3 _roamOrigin;
    private float _roamWaitTimer;
    private bool _isWaitingAtRoamPoint;
    private bool _hasDoneFirstMove;
    private bool _hasEnteredStateThisActivation;

    // Phase 1: SolidObstacle has just set a deterministic away-from-wall
    // destination on the agent. While this is true, the FSM holds off
    // re-issuing its own destination (mainly Chase, which otherwise re-aims
    // at the target every frame) and just waits for the agent to actually
    // arrive there.
    private bool _isRerouting;

    // Phase 2: the enemy has arrived at the away-from-wall point and is now
    // walking to a randomized point further away, so it doesn't immediately
    // walk straight back toward the wall/target along the same line.
    private bool _isDetouring;

    // True once phase 2 has actually managed to find and set a valid
    // randomized destination. Until this is true, "arrived" checks are not
    // evaluated - otherwise a failed NavMesh sample would leave the agent's
    // old, already-reached path in place and get misread as "already
    // arrived", ending the detour immediately and snapping the enemy
    // straight back onto the heading it was just pushed away from.
    private bool _detourDestinationSet;

    // Direction (horizontal, normalized) SolidObstacle just pushed the enemy
    // in, captured the moment NotifyObstacleReroute() is called - i.e.
    // straight away from the wall. The randomized detour point in phase 2 is
    // biased to stay within a cone around this direction, so the "random"
    // part only varies where within open space the enemy goes, rather than
    // ever risking a point that curves back toward the wall/target.
    private Vector3 _rerouteAwayDirection;

    // Safety countdown for whichever reroute phase is currently active.
    private float _rerouteWaitTimer;

    // The exact spot this enemy was standing when it entered State.Stay and
    // got deactivated. Clones must spawn around THIS point, not whatever
    // transform.position happens to read when CloneSelf() actually runs -
    // captured once here so a delayed clone spawn (after this object has
    // been inactive for a while) can never drift from where it "died"/stopped.
    private Vector3 _deactivationPosition;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _roamOrigin = transform.position;
        currentHP = maxHP;

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // If ResetForSpawn() already ran this activation (e.g. the spawner
        // called it right after SetActive(true)), Unity's deferred Start()
        // call would otherwise re-enter Roam and stomp the first-move
        // destination it just set. Only enter Roam here if nothing beat us
        // to it.
        if (_hasEnteredStateThisActivation)
            return;

        EnterState(State.Roam);
    }

    private void OnDisable()
    {
        _hasEnteredStateThisActivation = false;
        goingDown = false;
        _isRerouting = false;
        _isDetouring = false;
        _detourDestinationSet = false;
        _rerouteAwayDirection = Vector3.zero;
        _rerouteWaitTimer = 0f;
    }

    private void Update()
    {
        // Dead enemies stay exactly where they died and do nothing else -
        // no roaming, chasing, rerouting/detouring, or any other FSM logic.
        // We deliberately return here instead of deactivating the
        // GameObject, so the corpse remains visible/in-place in the scene.
        if (isDead)
            return;

        if (_isRerouting)
        {
            _rerouteWaitTimer -= Time.deltaTime;

            if (HasArrivedAtDestination() || _rerouteWaitTimer <= 0f)
            {
                // The enemy has actually reached the deterministic
                // away-from-wall point (or we gave up waiting for it) - now
                // try to pick a randomized point further in that same
                // general direction so it keeps moving away instead of
                // walking straight back the way it came.
                _isRerouting = false;
                BeginRerouteDetour();
            }
        }
        else if (_isDetouring)
        {
            _rerouteWaitTimer -= Time.deltaTime;

            if (!_detourDestinationSet)
            {
                // The first sample attempt(s) may have failed (common right
                // next to a wall, where part of the sample cone can be off
                // the NavMesh). Keep retrying every frame instead of
                // silently giving up and falling through to "arrived".
                _detourDestinationSet = TryPickDetourPoint();

                if (!_detourDestinationSet && _rerouteWaitTimer <= 0f)
                {
                    // Never found a valid point in time - stop trying and
                    // hand control back rather than stalling forever.
                    _isDetouring = false;
                    EndRerouteDetour();
                }
            }
            else if (HasArrivedAtDestination() || _rerouteWaitTimer <= 0f)
            {
                _isDetouring = false;
                EndRerouteDetour();
            }
        }

        switch (_currentState)
        {
            case State.Roam:
                UpdateRoam();
                break;
            case State.Chase:
                UpdateChase();
                break;
            case State.Stay:
                break;
        }
    }

    private bool HasArrivedAtDestination()
    {
        if (_agent.pathPending)
            return false;

        if (!_agent.hasPath)
            return true;

        return _agent.remainingDistance <= rerouteArriveDistance;
    }

    private void EnterState(State newState)
    {
        _currentState = newState;
        _hasEnteredStateThisActivation = true;

        switch (newState)
        {
            case State.Roam:
                _agent.speed = roamSpeed;
                _isWaitingAtRoamPoint = false;
                SetNewRoamDestination();
                break;
            case State.Chase:
                // An enemy that's actively chasing should never be treated as
                // "going down" - defensive reset in case goingDown was still
                // true from a stale spawn/serialization snapshot (see
                // ResetForSpawn for the main fix).
                goingDown = false;
                _agent.speed = chaseSpeed;
                break;
            case State.Stay:
                // Capture exactly where we are right now, before ResetPath/
                // deactivation/anything else has a chance to touch the
                // transform. This is the position clones will spawn around,
                // whether they spawn immediately below or after a delay.
                _deactivationPosition = transform.position;
                _agent.ResetPath();
                InfectTarget();
                ScheduleCloneAfterDelay();
                gameObject.SetActive(false);
                break;
        }
    }

    private void UpdateRoam()
    {
        if (TryDetectTarget())
        {
            goingDown = false;
            EnterState(State.Chase);
            return;
        }

        // While rerouting/detouring around an obstacle, let that movement
        // play out - don't let the normal roam wait/arrive flow fight over
        // the agent's destination in the meantime.
        if (_isRerouting || _isDetouring)
            return;

        if (_isWaitingAtRoamPoint)
        {
            _roamWaitTimer -= Time.deltaTime;
            if (_roamWaitTimer <= 0f)
            {
                _isWaitingAtRoamPoint = false;
                SetNewRoamDestination();
            }
            return;
        }

        if (!_agent.pathPending && _agent.hasPath && _agent.remainingDistance <= arriveDistance)
        {
            _agent.ResetPath();
            _isWaitingAtRoamPoint = true;
            _roamWaitTimer = roamPointWaitTime;
            goingDown = false;
        }
    }

    private void UpdateChase()
    {
        if (target == null)
        {
            EnterState(State.Roam);
            return;
        }

        IsInfected targetInfected = target.GetComponentInParent<IsInfected>();
        if (targetInfected != null && targetInfected.Infected)
        {
            target = null;
            EnterState(State.Roam);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget > loseTargetRadius)
        {
            EnterState(State.Roam);
            return;
        }

        if (distanceToTarget <= chaseArriveDistance)
        {
            EnterState(State.Stay);
            return;
        }

        // While SolidObstacle's deterministic away-from-wall push (or the
        // randomized follow-up point) is being walked to, don't stomp it by
        // re-aiming straight at the target every frame - let the agent
        // actually clear the wall and finish the detour first, then resume
        // chasing.
        if (_isRerouting || _isDetouring)
            return;

        _agent.SetDestination(target.position);
    }

    private bool TryDetectTarget()
    {
        if (target == null)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, targetLayer);
            foreach (Collider hit in hits)
            {
                IsInfected infectedCheck = hit.GetComponentInParent<IsInfected>();
                if (infectedCheck != null && infectedCheck.Infected)
                    continue;

                target = hit.transform;
                break;
            }
        }

        if (target == null)
            return false;

        IsInfected targetInfected = target.GetComponentInParent<IsInfected>();
        if (targetInfected != null && targetInfected.Infected)
        {
            target = null;
            return false;
        }

        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance > detectionRadius)
            return false;

        if (useLineOfSight)
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, toTarget.normalized, out RaycastHit hitInfo, distance, obstructionMask))
            {
                if (hitInfo.transform != target)
                    return false;
            }
        }

        return true;
    }

    private void SetNewRoamDestination()
    {
        Vector3 destinationPoint;
        float sampleDistance;

        if (performFirstMove && !_hasDoneFirstMove)
        {
            float firstMoveDistance = Random.Range(firstMoveDistanceMin, firstMoveDistanceMax);
            destinationPoint = _roamOrigin + firstMoveDirection.normalized * firstMoveDistance;
            sampleDistance = firstMoveDistance;
            _hasDoneFirstMove = true;
            goingDown = true;
        }
        else
        {
            destinationPoint = _roamOrigin + Random.insideUnitSphere * roamRadius;
            sampleDistance = roamRadius;
        }

        if (NavMesh.SamplePosition(destinationPoint, out NavMeshHit navHit, sampleDistance, NavMesh.AllAreas))
        {
            _agent.SetDestination(navHit.position);
        }
    }

    // Kicks off the delayed clone spawn on an external runner object so the
    // delay keeps ticking even after this GameObject has been deactivated.
    private void ScheduleCloneAfterDelay()
    {
        if (cloneSpawnDelay <= 0f)
        {
            // No delay requested - just spawn immediately before we deactivate.
            CloneSelf();
            return;
        }

        GameObject runnerObj = new GameObject("CloneSpawnRunner");
        runnerObj.transform.position = _deactivationPosition;
        CloneSpawnRunner runner = runnerObj.AddComponent<CloneSpawnRunner>();
        runner.Begin(this, cloneSpawnDelay);
    }

    // Called by CloneSpawnRunner once the delay has elapsed. Public so the
    // runner (a separate component) can invoke it even though this object
    // is inactive by that point.
    public void SpawnClonesAfterDelayElapsed()
    {
        CloneSelf();
    }

    // Allows external callers (e.g. cloning) to control whether this
    // instance should perform the initial "first move" before roaming.
    public void SetPerformFirstMove(bool value)
    {
        performFirstMove = value;
    }

    // Whether this enemy is currently performing its initial "first move"
    // (going down). External systems (e.g. SolidObstacle) can check this
    // to decide whether it's safe to reroute the enemy's NavMeshAgent.
    public bool GoingDown => goingDown;

    // Whether this enemy is dead. External systems (e.g. MeleeAttack2's
    // "eat" logic) need to check this the same way they check
    // DetectionFSM.currentHealth/currentState, since isDead itself is private.
    public bool IsDead => isDead;

    // Marks/unmarks this enemy, mirroring DetectionFSM's SetMarked/isMarked
    // so targeting systems (e.g. SuperMove) can treat both enemy types the
    // same way.
    public void SetMarked(bool value)
    {
        isMarked = value;
    }

    // Clears this enemy's mark. Mirrors DetectionFSM.ClearMark() so callers
    // don't need to special-case which enemy type they're clearing.
    public void ClearMark()
    {
        isMarked = false;
    }

    // Called by SolidObstacle right after it successfully reroutes this
    // enemy's NavMeshAgent around a wall. SolidObstacle's own push is always
    // a deterministic direction straight away from the wall (off the nearest
    // face normal) - that part is intentionally NOT randomized here.
    //
    // This captures that push direction (from the agent's current
    // destination, which SolidObstacle has just set), then tells the FSM to
    // (1) stop overwriting the agent's destination and (2) wait until the
    // enemy has actually ARRIVED at that away-from-wall point. Once it
    // arrives, the FSM picks a randomized point biased to continue further
    // in roughly that same away direction, waits for the enemy to arrive
    // there too, and only then hands control back to normal Roam/Chase - so
    // the enemy ends up somewhere genuinely different instead of curving
    // back toward the wall/target it just got pushed away from.
    public void NotifyObstacleReroute()
    {
        Vector3 diff = _agent.destination - transform.position;
        diff.y = 0f;
        _rerouteAwayDirection = diff.sqrMagnitude > 0.0001f ? diff.normalized : Vector3.zero;

        // A fresh reroute always takes priority over a detour already in
        // progress.
        _isRerouting = true;
        _isDetouring = false;
        _detourDestinationSet = false;
        _rerouteWaitTimer = rerouteMaxWaitTime;
    }

    // Called once the enemy has arrived at SolidObstacle's away-from-wall
    // point. Starts phase 2 and makes the first attempt at finding a valid
    // randomized point further away (further attempts, if needed, happen in
    // Update() via TryPickDetourPoint()).
    private void BeginRerouteDetour()
    {
        _isDetouring = true;
        _rerouteWaitTimer = rerouteMaxWaitTime;
        _detourDestinationSet = TryPickDetourPoint();
    }

    // Attempts to find a NavMesh point to continue the detour to, and if
    // found, sets it as the agent's destination.
    //
    // The point is NOT chosen from a fully random direction - that was the
    // original bug, since a plain random point around the enemy's position
    // (right next to the wall it just left) had a good chance of landing
    // back toward the wall or the target, making the enemy look like it was
    // stuck heading one direction. Instead, the direction is constrained to
    // a cone around _rerouteAwayDirection (the same direction SolidObstacle
    // just pushed the enemy in), so "random" only decides where within open
    // space it goes, never back toward the wall.
    //
    // DISTANCE: every normal attempt uses the SAME fixed distance
    // (rerouteRandomizeRadius), just with a different random angle within
    // the cone each time - so a successful detour reads as one consistent
    // measure no matter which attempt happened to land on valid NavMesh.
    // Only if every one of those fixed-distance attempts fails (e.g. a
    // genuinely tight corner) does this fall back to a few attempts at a
    // larger distance, purely so the enemy doesn't get stuck with no
    // detour at all - that fallback is the only case where the distance
    // should ever come out longer than usual.
    private bool TryPickDetourPoint()
    {
        if (!_agent.isOnNavMesh)
            return false;

        bool haveAwayDirection = _rerouteAwayDirection.sqrMagnitude > 0.0001f;

        // Normal attempts - fixed distance, so results are consistent.
        for (int attempt = 0; attempt < rerouteDetourSampleAttempts; attempt++)
        {
            if (TryDetourPointAtDistance(rerouteRandomizeRadius, haveAwayDirection, out Vector3 point))
            {
                _agent.SetDestination(point);
                return true;
            }
        }

        // Fallback only - every fixed-distance attempt above failed, so
        // widen the search rather than leaving the enemy with no detour.
        for (int attempt = 0; attempt < rerouteDetourFallbackAttempts; attempt++)
        {
            float fallbackDistance = rerouteRandomizeRadius * (2f + attempt);

            if (TryDetourPointAtDistance(fallbackDistance, haveAwayDirection, out Vector3 point))
            {
                _agent.SetDestination(point);
                return true;
            }
        }

        return false;
    }

    // Single sampling attempt at a specific distance: picks a direction
    // within the away-from-wall cone (or a random horizontal direction if
    // no away direction is known), samples the NavMesh at that distance,
    // and rejects anything that landed basically back where the enemy
    // already is.
    private bool TryDetourPointAtDistance(float distance, bool haveAwayDirection, out Vector3 point)
    {
        point = Vector3.zero;

        Vector3 direction;
        if (haveAwayDirection)
        {
            float angleOffset = Random.Range(-rerouteAwayBiasAngle, rerouteAwayBiasAngle);
            direction = Quaternion.Euler(0f, angleOffset, 0f) * _rerouteAwayDirection;
        }
        else
        {
            // No away direction available (edge case, e.g. agent had no
            // destination yet) - fall back to a random horizontal
            // direction rather than failing outright.
            Vector3 randomFlat = Random.insideUnitSphere;
            randomFlat.y = 0f;
            direction = randomFlat.sqrMagnitude > 0.0001f ? randomFlat.normalized : Vector3.forward;
        }

        Vector3 randomPoint = transform.position + direction * distance;

        if (!NavMesh.SamplePosition(randomPoint, out NavMeshHit navHit, distance, NavMesh.AllAreas))
            return false;

        // Reject points that landed right back where we're already
        // standing (can happen in tight corners) - that wouldn't read as a
        // real detour, just an instant "arrival".
        if (Vector3.Distance(navHit.position, transform.position) < rerouteArriveDistance)
            return false;

        point = navHit.position;
        return true;
    }

    // Called once the enemy has arrived at the randomized detour point (or
    // once we gave up trying to find/reach one). Hands control back to
    // whichever state is currently active.
    private void EndRerouteDetour()
    {
        if (_currentState == State.Roam)
        {
            // Resume normal roaming with a fresh destination rather than
            // re-using the detour point or waiting on stale path state.
            _isWaitingAtRoamPoint = false;
            SetNewRoamDestination();
        }

        // If Chase, nothing else to do here - the next UpdateChase() call
        // will resume aiming straight at the target now that _isRerouting
        // and _isDetouring are both false.
    }

    private void CloneSelf()
    {
        int cloneCount = Random.Range(minCloneCount, maxCloneCount + 1);

        for (int i = 0; i < cloneCount; i++)
        {
            Vector3 spawnOffset = Random.insideUnitSphere * cloneSpawnRadius;
            spawnOffset.y = 0f;

            // Always spawn around the captured deactivation point, not
            // transform.position - by the time a delayed clone spawn fires,
            // this object has been inactive (possibly for a while), and we
            // want clones appearing exactly where it stopped, not wherever
            // its transform happens to read at that later moment.
            Vector3 spawnPosition = _deactivationPosition + spawnOffset;

            if (!NavMesh.SamplePosition(spawnPosition, out NavMeshHit navHit, cloneSpawnRadius, NavMesh.AllAreas))
                continue;

            GameObject clone = Instantiate(gameObject, navHit.position, transform.rotation);

            // Since the source object may already be inactive by the time we
            // spawn (delayed clone), explicitly ensure the clone is active.
            clone.SetActive(true);

            InfluenzaFSM cloneFsm = clone.GetComponent<InfluenzaFSM>();

            if (cloneFsm != null)
            {
                // Clones spawn already near their parent - they shouldn't
                // repeat the initial "first move" trek.
                cloneFsm.SetPerformFirstMove(false);
                cloneFsm.ResetForSpawn();
            }
        }
    }

    public void ResetForSpawn()
    {
        target = null;
        isDead = false;
        currentHP = maxHP;
        _hasDoneFirstMove = false;
        _roamOrigin = transform.position;

        if (animator != null)
            animator.SetBool("Dead", false);

        // Don't inherit a stale "true" value from Instantiate's serialized
        // snapshot of the source object. Without this, a clone could boot up
        // already considered "going down" indefinitely (since performFirstMove
        // is forced false for clones via SetPerformFirstMove, nothing in the
        // normal roam flow would ever flip goingDown back to false for it),
        // which made SolidObstacle skip rerouting it forever even with the
        // Inspector checkbox unticked.
        goingDown = false;
        _isRerouting = false;
        _isDetouring = false;
        _detourDestinationSet = false;
        _rerouteAwayDirection = Vector3.zero;
        _rerouteWaitTimer = 0f;

        // A cloned enemy should never inherit the parent's mark - it's a
        // separate, freshly-spawned target that hasn't been marked yet.
        isMarked = false;

        EnterState(State.Roam);
    }

    private void InfectTarget()
    {
        if (target == null)
            return;

        IsInfected infected = target.GetComponentInParent<IsInfected>();
        if (infected != null)
        {
            infected.SetInfected(true);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

        currentHP -= amount;

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    // Death now leaves the enemy exactly where it died, doing nothing else -
    // it no longer deactivates the GameObject. The agent is stopped in place
    // (path cleared, movement halted) so it doesn't keep sliding/animating
    // toward wherever it was last headed, and Update() bails out early via
    // the isDead check at the top so no FSM logic (roam/chase/reroute/etc.)
    // runs anymore.
    private void Die()
    {
        isDead = true;

        if (animator != null)
            animator.SetBool("Dead", true);

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
        }

        // Report this kill to the star-rating tracker (WBC / EnemyKilled).
        // Guarded so enemies without a valuesForStar assigned (or scenes
        // that don't use the star system) don't throw.
        if (valuesForStar != null)
        {
            valuesForStar.ReportEnemyKilled();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseTargetRadius);
    }
}

// Small standalone helper that outlives the InfluenzaFSM's deactivation.
// It just waits out the clone spawn delay, then tells the (now inactive)
// InfluenzaFSM instance to spawn its clones, and cleans itself up.
public class CloneSpawnRunner : MonoBehaviour
{
    private InfluenzaFSM _source;
    private float _delay;

    public void Begin(InfluenzaFSM source, float delay)
    {
        _source = source;
        _delay = delay;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        yield return new WaitForSeconds(_delay);

        if (_source != null)
        {
            _source.SpawnClonesAfterDelayElapsed();
        }

        Destroy(gameObject);
    }
}