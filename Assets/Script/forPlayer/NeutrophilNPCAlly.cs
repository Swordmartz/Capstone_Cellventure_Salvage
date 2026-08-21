using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// FSM for a friendly NPC that fights alongside the player.
///
///   Roam   -> wanders randomly within roamRadius of its spawn point, pausing
///             at each destination before picking a new one. Default state.
///   Chase  -> once an enemy is detected within detectionRadius (via
///             Physics.OverlapSphere on the given layer + optional tag), the
///             NPC moves toward it, re-pathing periodically.
///   Attack -> once within engagement range, the NPC stops moving, faces
///             the target, and calls the appropriate attack script on a
///             cooldown for as long as the target stays in range - Super if
///             the target is marked, Melee if it's within attackRange,
///             otherwise Ranged (PlayerShooter) if it's farther but still
///             within shootRange.
///
/// Detection runs on its own timer independent of state (while not already
/// chasing/attacking), so it keeps checking while roaming without doing an
/// OverlapSphere every single frame.
///
/// Requires the NPC to already be placed on a baked NavMesh.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NeutrophilNPCAlly : MonoBehaviour
{
    private enum State
    {
        Roam,
        Chase,
        Attack
    }

    [Header("State (read-only, for debugging)")]
    [SerializeField] private State currentState = State.Roam;

    [Header("Roaming")]
    [Tooltip("How far from this NPC's spawn point it will wander.")]
    [SerializeField] private float roamRadius = 8f;

    [Tooltip("How long the NPC waits at each roam destination before picking a new one.")]
    [SerializeField] private float roamWaitTime = 2f;

    [Tooltip("NavMeshAgent speed while roaming.")]
    [SerializeField] private float roamSpeed = 2f;

    [Header("Detection")]
    [Tooltip("Radius (in world units) within which the NPC scans for an enemy.")]
    [SerializeField] private float detectionRadius = 10f;

    [Tooltip("Only colliders on this layer are considered by the detection OverlapSphere. Set this to your enemy layer.")]
    [SerializeField] private LayerMask enemyLayer;

    [Tooltip("Optional. If set, only colliders with this tag are treated as a valid target. Leave empty to skip the tag check.")]
    [SerializeField] private string requiredTag = "";

    [Tooltip("How often (in seconds) the NPC checks for a target while not already chasing/attacking.")]
    [SerializeField] private float detectionCheckInterval = 0.25f;

    [Header("Chasing")]
    [Tooltip("NavMeshAgent speed while chasing.")]
    [SerializeField] private float chaseSpeed = 4f;

    [Tooltip("How often (in seconds) the destination is refreshed to the target's current position while chasing.")]
    [SerializeField] private float chaseRepathInterval = 0.15f;

    [Tooltip("If the target gets farther than this while chasing, give up and return to roaming.")]
    [SerializeField] private float loseTargetRadius = 16f;

    [Header("Attack")]
    [Tooltip("Distance at which the NPC stops moving and switches from PlayerShooter to MeleeAttack.")]
    [SerializeField] private float attackRange = 1.5f;

    [Tooltip("Outer distance at which the NPC will engage at all - stops moving and starts using " +
             "PlayerShooter once the target is within this range (even if farther than attackRange). " +
             "Should be >= attackRange; if it's smaller, attackRange is used as the effective value.")]
    [SerializeField] private float shootRange = 6f;

    [Tooltip("Seconds between each attack while the target stays in range.")]
    [SerializeField] private float attackCooldown = 1f;

    [Tooltip("If true, the NPC rotates to face the target while attacking.")]
    [SerializeField] private bool faceTargetWhileAttacking = true;

    [Header("Arrival / Stopping")]
    [Tooltip("NavMeshAgent.stoppingDistance. Left at 0 this can cause the agent to endlessly " +
             "creep/jitter trying to close the last few centimeters to a destination, since " +
             "remainingDistance rarely hits exactly 0 due to navmesh/path floating point slop. " +
             "This is applied to agent.stoppingDistance at Awake.")]
    [SerializeField] private float stoppingDistance = 0.15f;

    [Tooltip("While roaming, also treat the destination as 'reached' once within this distance, " +
             "as a belt-and-suspenders check in case remainingDistance/pathPending report stale " +
             "values for a frame (e.g. right after SetDestination).")]
    [SerializeField] private float roamArrivalTolerance = 0.25f;

    [Header("Punch Script Hooks")]
    [Tooltip("The MeleeAttack component on this NPC. Called each attack tick when the target is " +
             "within attackRange and not marked. If left empty, the script will try " +
             "GetComponent<MeleeAttack>() at Awake.")]
    [SerializeField] private MeleeAttack meleeAttack;

    [Tooltip("The PlayerShooter component on this NPC. Called each attack tick when the target is " +
             "farther than attackRange but still within shootRange, and not marked. If left empty, " +
             "the script will try GetComponent<PlayerShooter>() at Awake.")]
    [SerializeField] private PlayerShooter playerShooter;

    [Tooltip("The SuperMove component on this NPC. Called each attack tick whenever the current " +
             "target is marked, regardless of distance (takes priority over Melee/Ranged). If left " +
             "empty, the script will try GetComponent<SuperMove>() at Awake. Note " +
             "SuperMove.ActivateSuper() does its own gating (superBar.IsFull and a marked enemy in " +
             "range) - calling it here is harmless if either condition isn't met, it just no-ops.")]
    [SerializeField] private SuperMove superMove;

    [Range(0f, 1f)]
    [Tooltip("Chance (0-1) of using Super on a given attack tick when the target is marked, instead " +
             "of always using it. A high value (e.g. 0.7-0.85) still uses Super most of the time but " +
             "occasionally lets Melee/Ranged play instead, rather than Super being 100% guaranteed " +
             "the moment a target is marked.")]
    [SerializeField] private float superUseChance = 0.75f;

    [Header("Facing / Look Indicator")]
    [Tooltip("Optional. If assigned, the Animator's LastX/LastY floats are updated with the current " +
             "facing direction (X/Z plane) instead of rotating this NPC's own transform - keeps this " +
             "script from fighting a billboard script that's also driving transform.rotation every frame.")]
    [SerializeField] private Animator animator;

    [Tooltip("Optional. A separate child transform (e.g. an arrow/indicator sprite) that gets rotated to " +
             "point toward whatever this NPC is currently facing - movement direction while roaming/chasing, " +
             "target direction while attacking. This is rotated directly since it's NOT the object a billboard " +
             "script would be controlling, so it's safe to spin freely without any conflict. If left unassigned, " +
             "the script auto-creates a simple default indicator (a short colored line) at Awake.")]
    [SerializeField] private Transform lookIndicator;

    [Tooltip("Height above this NPC's base position the auto-created default indicator is placed at " +
             "(only used if lookIndicator is left unassigned).")]
    [SerializeField] private float autoIndicatorHeight = 1.5f;

    [Tooltip("Length of the auto-created default indicator line (only used if lookIndicator is left unassigned).")]
    [SerializeField] private float autoIndicatorLength = 1f;

    [Tooltip("Color of the auto-created default indicator line (only used if lookIndicator is left unassigned).")]
    [SerializeField] private Color autoIndicatorColor = Color.cyan;

    // True if lookIndicator above was auto-created by this script rather than
    // assigned in the Inspector - controls whether Update() syncs its
    // position every frame and whether OnDestroy() cleans it up.
    private bool ownsLookIndicator;

    [Tooltip("Minimum agent speed (units/sec) before a movement direction counts as 'facing-worthy'. " +
             "NavMeshAgent.velocity is noisy at very low speeds (starting/stopping, weaving around small " +
             "obstacles) - filtering that out here stops the facing direction from flickering/jittering.")]
    [SerializeField] private float minSpeedToUpdateFacing = 0.3f;

    [Tooltip("How fast (degrees/sec) lookIndicator turns to catch up to the target facing direction, " +
             "instead of snapping instantly - smooths out any residual per-frame direction noise.")]
    [SerializeField] private float lookIndicatorTurnSpeed = 540f;

    private static readonly int LastXHash = Animator.StringToHash("LastMoveX");
    private static readonly int LastYHash = Animator.StringToHash("LastMoveY");

    private NavMeshAgent agent;
    private Transform target;
    private Vector3 roamOrigin;

    private bool isWaitingAtRoamPoint;
    private float roamWaitTimer;

    private float detectionTimer;
    private float chaseRepathTimer;
    private float attackTimer;

    // Set whenever this NPC is (re)activated (e.g. SetActive(true) after
    // starting inactive, or pulled from a pool). Right after activation the
    // NavMeshAgent isn't guaranteed to be placed on the NavMesh yet on that
    // same frame - agent.velocity/remainingDistance etc. can report garbage
    // until it is, which is what was causing the facing indicator to jitter
    // immediately on activation. This defers entering Roam (and running any
    // facing/movement logic) until the agent confirms it's actually on the
    // mesh, retrying every frame rather than assuming a fixed order between
    // whatever activates this object and whatever positions its agent.
    private bool pendingActivationReset;
    private float pendingActivationTimer;
    private bool warnedAboutStuckActivation;

    // Once Super has actually been triggered, this NPC stops attacking
    // entirely - Super kills the target and switches to the next character
    // (see SuperMove.SwitchToNextCharacter/FinishSwitchAfterDelay), so
    // further Melee/Ranged/Super calls from this ally don't make sense in
    // the window before that switch/deactivation actually completes.
    private bool hasUsedSuper;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        roamOrigin = transform.position;

        // NavMeshAgent's own "Auto Rotate" (updateRotation) directly drives
        // transform.rotation to face movement direction by default - if a
        // billboard script is ALSO setting transform.rotation every frame
        // (to face the camera), the two fight over the same transform and
        // the whole NPC visibly jitters in place. This script already keeps
        // its own facing logic off transform.rotation entirely (see
        // UpdateFacingVisual) - this line makes sure the agent does too,
        // leaving transform.rotation fully owned by the billboard script.
        agent.updateRotation = false;

        // Left at the NavMeshAgent default of 0, remainingDistance almost
        // never reaches exactly 0 (navmesh edge / path corner floating
        // point slop), so the "have we arrived?" checks below can stall
        // forever with the agent creeping/jittering the last few
        // centimeters toward a destination it technically never "reaches".
        // Applying a small buffer here fixes that for both roaming and any
        // other SetDestination() calls this agent makes.
        agent.stoppingDistance = stoppingDistance;

        // A non-kinematic Rigidbody on this object (gravity, collisions,
        // physics pushes) fights the NavMeshAgent for control of
        // transform.position every FixedUpdate, which is one of the most
        // common causes of "creeps forward and jitters in place, never
        // arrives". If this NPC has a Rigidbody, force it kinematic so the
        // agent is the sole owner of position - remove the Rigidbody
        // entirely instead if you don't actually need physics on it.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            Debug.LogWarning($"[NeutrophilNPCAlly] {name}: found a non-kinematic Rigidbody, which " +
                "will fight NavMeshAgent for control of position. Forcing it kinematic.");
            rb.isKinematic = true;
        }

        if (meleeAttack == null)
            meleeAttack = GetComponent<MeleeAttack>();

        if (playerShooter == null)
            playerShooter = GetComponent<PlayerShooter>();

        if (superMove == null)
            superMove = GetComponent<SuperMove>();

        // If Apply Root Motion is enabled on the Animator, the currently
        // playing clip (idle/attack/etc.) also nudges transform.position and
        // rotation based on its baked-in motion curves - fighting
        // NavMeshAgent for control of the same transform every frame, even
        // while stationary. That fight is a very common cause of an
        // in-place jitter like this. Disabling it here means the agent (for
        // position) and the billboard script (for rotation) are the only
        // things touching this transform.
        if (animator != null)
            animator.applyRootMotion = false;

        if (lookIndicator == null)
        {
            lookIndicator = CreateDefaultLookIndicator();
            ownsLookIndicator = true;
        }
    }

    private void OnEnable()
    {
        // Don't touch state directly here (see pendingActivationReset comment
        // above) - just flag that a reset is due, and let Update() apply it
        // once the agent is confirmed to actually be on the NavMesh.
        pendingActivationReset = true;
        pendingActivationTimer = 0f;
        warnedAboutStuckActivation = false;
        hasUsedSuper = false;
    }

    private void Update()
    {
        if (pendingActivationReset)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                target = null;
                isWaitingAtRoamPoint = false;
                roamOrigin = transform.position;

                EnterRoam();
                pendingActivationReset = false;
            }
            else
            {
                // Not confirmed on the NavMesh yet this frame - skip
                // everything else and try again next Update rather than
                // running movement/facing logic against a not-yet-ready agent.
                // If this never resolves, the whole FSM (including
                // detection) silently never runs - warn loudly once so
                // that's obvious instead of just "nothing happens".
                pendingActivationTimer += Time.deltaTime;
                if (!warnedAboutStuckActivation && pendingActivationTimer > 2f)
                {
                    warnedAboutStuckActivation = true;
                    Debug.LogWarning($"[NeutrophilNPCAlly] {name}: agent.isOnNavMesh has been false for " +
                        "2+ seconds - the FSM (including enemy detection) will never run until this NPC " +
                        "is actually placed on a baked NavMesh. Check: (1) the scene has baked NavMesh " +
                        "covering this NPC's position, (2) the NavMeshAgent's Agent Type matches a baked " +
                        "NavMesh, (3) this NPC didn't spawn floating above/below the NavMesh surface " +
                        "(check Base Offset / Height on the NavMeshAgent), and (4) it wasn't spawned via " +
                        "Instantiate() far from any NavMesh (agent.Warp() may be needed in that case).");
                }
                return;
            }
        }

        // Keep scanning for a target while roaming, but not while already
        // chasing/attacking one - TryDetectTarget() also no-ops once target
        // is set, this just avoids the OverlapSphere call entirely.
        if (currentState == State.Roam)
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
            case State.Attack:
                UpdateAttack();
                break;
        }

        // Attack state handles its own facing (toward the target) inside
        // UpdateAttack() - for Roam/Chase, face whatever direction the
        // agent is actually moving in.
        if (currentState != State.Attack)
        {
            UpdateFacingVisual(agent.velocity);
        }

        // The auto-created indicator isn't parented to this NPC (see
        // CreateDefaultLookIndicator), so its position has to be synced
        // manually every frame instead of following via the transform
        // hierarchy. A user-assigned lookIndicator is left alone here -
        // if it's parented as a child, normal Unity parenting already
        // keeps its position in sync.
        if (ownsLookIndicator && lookIndicator != null)
        {
            lookIndicator.position = transform.position + Vector3.up * autoIndicatorHeight;
        }
    }

    // ---------------------------------------------------------------
    // Detection
    // ---------------------------------------------------------------

    private void TryDetectTarget()
    {
        if (target != null)
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
        Debug.Log($"[NeutrophilNPCAlly] {name}: detection scan found {hits.Length} collider(s) on enemyLayer within {detectionRadius}.");

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            Debug.Log($"[NeutrophilNPCAlly] {name}: candidate '{hit.name}' on layer '{LayerMask.LayerToName(hit.gameObject.layer)}', tag '{hit.tag}'.");

            if (!string.IsNullOrEmpty(requiredTag) && !hit.CompareTag(requiredTag))
            {
                Debug.Log($"[NeutrophilNPCAlly] {name}: '{hit.name}' rejected - tag doesn't match requiredTag '{requiredTag}'.");
                continue;
            }

            target = hit.transform;
            Debug.Log($"[NeutrophilNPCAlly] {name}: acquired target '{hit.name}'.");
            EnterChase();
            return;
        }
    }

    private bool IsTargetValid()
    {
        return target != null && target.gameObject.activeInHierarchy;
    }

    // ---------------------------------------------------------------
    // Roam State
    // ---------------------------------------------------------------

    private void EnterRoam()
    {
        currentState = State.Roam;
        agent.isStopped = false;
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

        // Two checks combined: the "official" NavMeshAgent arrival check
        // (remainingDistance <= stoppingDistance), plus a raw straight-line
        // distance fallback (roamArrivalTolerance). The fallback catches
        // cases where remainingDistance/pathPending report stale data for a
        // frame or two (e.g. immediately after SetDestination, or on a very
        // short path) - without it the agent can sit there re-evaluating a
        // "not yet arrived" path every frame, which reads as jitter.
        bool reachedByAgent = !agent.pathPending
            && agent.remainingDistance <= agent.stoppingDistance;
        bool reachedByDistance = !agent.pathPending
            && Vector3.Distance(transform.position, agent.destination) <= roamArrivalTolerance;

        if (reachedByAgent || reachedByDistance)
        {
            isWaitingAtRoamPoint = true;
            roamWaitTimer = roamWaitTime;
        }
    }

    private void PickNewRoamDestination()
    {
        // NavMesh.SamplePosition only checks proximity to ANY NavMesh
        // surface - it does NOT check whether that point is actually
        // reachable/connected from where this agent currently stands. If a
        // sampled point lands on a disconnected island (across a gap, other
        // side of a wall, etc.), SetDestination() still accepts it, but the
        // agent can only path to the nearest reachable spot on its own
        // island and gets stuck endlessly recalculating a path that never
        // actually goes anywhere - which looks exactly like jittering in
        // place. So each candidate is validated with CalculatePath() before
        // being committed to, and a few candidates are tried before giving
        // up for this cycle.
        const int maxAttempts = 5;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Horizontal circle rather than insideUnitSphere so multi-floor
            // maps don't sample a point above/below the current floor.
            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            Vector3 randomPoint = roamOrigin + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (!NavMesh.SamplePosition(randomPoint, out NavMeshHit navHit, roamRadius, NavMesh.AllAreas))
                continue;

            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(navHit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetDestination(navHit.position);
                return;
            }
        }

        // Every candidate this cycle was unreachable - don't commit to a
        // bad destination. Shorten the wait so the NPC retries again soon
        // rather than sitting idle for the full roamWaitTime, and log once
        // so it's visible in the Console that this is happening (e.g. the
        // NPC is on a small/disconnected NavMesh island).
        isWaitingAtRoamPoint = true;
        roamWaitTimer = Mathf.Min(roamWaitTime, 0.5f);
        Debug.LogWarning($"[NeutrophilNPCAlly] {name}: no reachable roam point found within " +
            $"{roamRadius} units after {maxAttempts} attempts - check for NavMesh gaps/disconnected " +
            "islands near this NPC's spawn point.");
    }

    // ---------------------------------------------------------------
    // Chase State
    // ---------------------------------------------------------------

    private void EnterChase()
    {
        currentState = State.Chase;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        chaseRepathTimer = 0f;
    }

    private void UpdateChase()
    {
        if (!IsTargetValid())
        {
            Debug.Log($"[NeutrophilNPCAlly] {name}: chase target became invalid (null or deactivated) - returning to Roam.");
            target = null;
            EnterRoam();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > loseTargetRadius)
        {
            Debug.Log($"[NeutrophilNPCAlly] {name}: target '{target.name}' is {distance:F1} away, past loseTargetRadius {loseTargetRadius} - giving up and returning to Roam.");
            target = null;
            EnterRoam();
            return;
        }

        if (distance <= EngageRange)
        {
            Debug.Log($"[NeutrophilNPCAlly] {name}: target '{target.name}' is {distance:F1} away, within EngageRange {EngageRange} - entering Attack.");
            EnterAttack();
            return;
        }

        chaseRepathTimer -= Time.deltaTime;
        if (chaseRepathTimer <= 0f)
        {
            chaseRepathTimer = chaseRepathInterval;

            // Validate the path to the target before committing, same as
            // PickNewRoamDestination() does for roam points. Without this,
            // an unreachable target (across a gap, behind a wall with no
            // navmesh connection, etc.) causes the agent to path toward the
            // nearest point it CAN reach and then endlessly recalculate a
            // path that never actually closes the distance - which looks
            // like slow crawling/jittering that never arrives.
            NavMeshPath path = new NavMeshPath();
            bool gotPath = agent.CalculatePath(target.position, path);
            Debug.Log($"[NeutrophilNPCAlly] {name}: repathing to '{target.name}' ({distance:F1} away) - CalculatePath returned {gotPath}, status {path.status}.");

            if (gotPath && path.status != NavMeshPathStatus.PathInvalid)
            {
                agent.SetDestination(target.position);
                Debug.Log($"[NeutrophilNPCAlly] {name}: SetDestination called. agent.isStopped={agent.isStopped}, agent.speed={agent.speed}, agent.isOnNavMesh={agent.isOnNavMesh}.");
            }
            else
            {
                Debug.LogWarning($"[NeutrophilNPCAlly] {name}: path to '{target.name}' is invalid/unreachable - the agent won't move. Check the target is actually on/near a baked NavMesh.");
            }
        }
    }

    // Outer distance at which the NPC will stop and start attacking at all -
    // shootRange normally, but never smaller than attackRange even if
    // shootRange was left misconfigured below it in the Inspector.
    private float EngageRange => Mathf.Max(attackRange, shootRange);

    // ---------------------------------------------------------------
    // Attack State
    // ---------------------------------------------------------------

    private void EnterAttack()
    {
        currentState = State.Attack;
        agent.ResetPath();
        agent.isStopped = true;

        // Punch immediately on entering range rather than waiting out a
        // full cooldown first.
        attackTimer = 0f;
    }

    private void UpdateAttack()
    {
        if (!IsTargetValid())
        {
            target = null;
            agent.isStopped = false;
            EnterRoam();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > EngageRange)
        {
            // Target moved out of range entirely (even ranged) - resume
            // chasing rather than dropping all the way back to roaming.
            agent.isStopped = false;
            EnterChase();
            return;
        }

        if (faceTargetWhileAttacking)
        {
            Vector3 lookDir = target.position - transform.position;
            UpdateFacingVisual(lookDir);
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            DoPunch();
        }
    }

    private void DoPunch()
    {
        if (!IsTargetValid())
            return;

        // Once Super has actually fired, this NPC is done attacking for
        // good - it kills the target and hands off to the next character,
        // so any further Melee/Ranged/Super calls in the window before
        // that switch/deactivation completes would just be wasted (or
        // could hit a target that's already dead/gone).
        if (hasUsedSuper)
            return;

        // Super is used most of the time when the target is marked, but not
        // guaranteed - rolled against superUseChance so Melee/Ranged still
        // get a chance to play even on a marked target, rather than Super
        // being a 100% lock the instant something's marked.
        //
        // ActivateSuper() itself can silently no-op (e.g. if superBar isn't
        // actually full) with no return value to tell us it failed, so
        // superBar.IsFull is checked here too before committing to
        // hasUsedSuper - otherwise a no-op call would permanently disable
        // this NPC's attacking for no reason.
        bool superReady = superMove != null && superMove.superBar != null && superMove.superBar.IsFull;

        if (superReady && IsTargetMarked() && Random.value <= superUseChance)
        {
            superMove.ActivateSuper();
            hasUsedSuper = true;
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        // Melee whenever the target is within attackRange, OR - even at
        // range - whenever PlayerShooter is on cooldown, so the NPC keeps
        // swinging instead of standing idle waiting for the ranged
        // cooldown to clear. (A melee swing while the target is still out
        // of attackRange just won't land a hit via MeleeAttack's own
        // OverlapSphere check, but it keeps the NPC actively attacking
        // rather than doing nothing each tick.)
        bool shooterOnCooldown = playerShooter != null && playerShooter.IsOnCooldown;

        if (distance <= attackRange || shooterOnCooldown)
        {
            if (meleeAttack != null)
                meleeAttack.PerformAttack();
        }
        else
        {
            if (playerShooter != null)
                playerShooter.Shoot();
        }
    }

    // Mirrors the per-type isMarked/IsMarked checks SuperMove.GetNearestKillableEnemy()
    // does, but against this NPC's own current target rather than scanning
    // for the nearest marked enemy - EnemySplineFollower isn't included
    // since SuperMove doesn't support marking/killing that type either.
    private bool IsTargetMarked()
    {
        if (target == null)
            return false;

        DetectionFSM detectionEnemy = target.GetComponent<DetectionFSM>();
        if (detectionEnemy != null)
            return detectionEnemy.isMarked;

        InfluenzaFSM influenzaEnemy = target.GetComponent<InfluenzaFSM>();
        if (influenzaEnemy != null)
            return influenzaEnemy.isMarked;

        pneumonococcalFSM pneumonococcalEnemy = target.GetComponent<pneumonococcalFSM>();
        if (pneumonococcalEnemy != null)
            return pneumonococcalEnemy.isMarked;

        MalariaFSM malariaEnemy = target.GetComponent<MalariaFSM>();
        if (malariaEnemy != null)
            return malariaEnemy.IsMarked;

        return false;
    }

    // ---------------------------------------------------------------
    // Facing (billboard-safe)
    // ---------------------------------------------------------------

    // Updates the Animator's LastX/LastY floats and rotates lookIndicator
    // (if assigned) to reflect the given direction on the X/Z plane. Only
    // updates while dir is actually non-zero - while idle/stationary, the
    // previous (non-zero) direction is kept rather than snapping to zero.
    //
    // Deliberately never touches this NPC's own transform.rotation - that's
    // left entirely to a billboard script (same convention as
    // EnemyPatrolFSM.UpdateFacingAnimator), so this can run every frame
    // without fighting it.
    // Creates a simple default look indicator (a short colored line via
    // LineRenderer) when lookIndicator is left unassigned in the Inspector.
    //
    // Deliberately NOT parented to this NPC's own transform - a child's
    // world rotation is derived from its parent's, so if this were a child
    // of the NPC, a billboard script rotating the NPC would indirectly spin
    // this indicator too, reintroducing the exact jitter this whole facing
    // system was built to avoid. Its position is instead synced manually
    // every frame in Update() (see ownsLookIndicator), and its rotation is
    // set entirely independently in UpdateFacingVisual().
    private Transform CreateDefaultLookIndicator()
    {
        GameObject indicatorObj = new GameObject(name + " LookIndicator (auto)");

        LineRenderer lr = indicatorObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, Vector3.forward * autoIndicatorLength);
        lr.startWidth = 0.05f;
        lr.endWidth = 0.02f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = autoIndicatorColor;
        lr.endColor = autoIndicatorColor;

        return indicatorObj.transform;
    }

    private void OnDestroy()
    {
        // Only clean up the indicator if this script created it - a
        // user-assigned lookIndicator belongs to the scene/prefab and isn't
        // ours to destroy.
        if (ownsLookIndicator && lookIndicator != null)
        {
            Destroy(lookIndicator.gameObject);
        }
    }

    private void UpdateFacingVisual(Vector3 dir)
    {
        dir.y = 0f;

        // Filter out low-speed noise (NavMeshAgent.velocity direction is
        // unstable when barely moving) rather than reacting to every tiny
        // fluctuation - below this speed, just keep whatever direction was
        // already locked in.
        if (dir.sqrMagnitude < minSpeedToUpdateFacing * minSpeedToUpdateFacing)
            return;

        dir.Normalize();

        // Both axes now need a sign flip relative to raw dir: X was correct
        // before but is now also inverted per request, and Y (driven from
        // world Z) already had its own extra flip on top of the base
        // negation below.
        dir = -dir;

        if (animator != null)
        {
            animator.SetFloat(LastXHash, dir.x);
            animator.SetFloat(LastYHash, -dir.z);
        }

        if (lookIndicator != null)
        {
            // Turn toward the target direction over time instead of
            // snapping instantly - smooths out any remaining per-frame
            // direction noise into a clean rotation instead of a jitter.
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            lookIndicator.rotation = Quaternion.RotateTowards(
                lookIndicator.rotation,
                targetRotation,
                lookIndicatorTurnSpeed * Time.deltaTime);
        }
    }

    // ---------------------------------------------------------------
    // Gizmos
    // ---------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, shootRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(Application.isPlaying ? roamOrigin : transform.position, roamRadius);
    }
}