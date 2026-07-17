using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Makes a BoxCollider act as a solid obstacle that keeps actors OUTSIDE of it,
/// even while an actor's own Collider.isTrigger is set to true (phasing), and
/// even for actors that don't use physics at all (NavMeshAgent-driven enemies).
///
/// WHY THIS EXISTS:
/// Normal Unity physics collision only blocks movement when BOTH colliders
/// are non-trigger. Since the player's collider toggles isTrigger on/off
/// while phasing through walls, relying on physics collision alone means
/// the player could phase straight through this object too. Enemies moved
/// by a NavMeshAgent don't respond to physics pushback at all (they have no
/// Rigidbody velocity to correct), so they need their own handling.
/// This script ignores the trigger/physics collision system entirely: every
/// frame it finds every actor currently overlapping the box (players,
/// enemies, anything on the configured layers) and pushes each one back out
/// along whichever face it penetrated least (i.e. the nearest edge), rather
/// than teleporting them all the way back to wherever they entered from.
///
/// SETUP:
/// 1. Add a BoxCollider to the obstacle GameObject, tick "Is Trigger"
///    (this script handles the actual blocking, not physics).
/// 2. Size/position the BoxCollider to match the obstacle's shape.
/// 3. Attach this script to that same GameObject.
/// 4. Set Affected Layers to include both the player's layer and the
///    enemies' layer, so both get detected and pushed back.
/// 5. Each actor needs its own Collider (trigger or not) for the overlap
///    query to find it - a Rigidbody actor (player) gets velocity-based
///    pushback, a NavMeshAgent actor (enemy) gets warped back out instead.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class SolidObstacle : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Which layers count as actors this obstacle should push back out. Should include both the player's layer and the enemies' layer.")]
    [SerializeField] private LayerMask affectedLayers = ~0;

    [Header("Pushback (Player)")]
    [Tooltip("Extra gap kept between an actor and the obstacle surface after a pushback, so they don't immediately re-trigger it.")]
    [SerializeField] private float surfacePadding = 0.05f;

    [Tooltip("How strong an outward velocity kick to apply on the pushback axis (bounce feel). Set to 0 for no kick. Rigidbody actors only.")]
    [SerializeField] private float pushBackForce = 4f;

    [Tooltip("If true, only the velocity component along the pushback axis is affected (Rigidbody actors only). If false, the actor's entire velocity is zeroed (old hard-stop behavior).")]
    [SerializeField] private bool onlyAffectPushAxisVelocity = true;

    [Header("Reroute (Enemies)")]
    [Tooltip("How far past the obstacle's surface (beyond the enemy's own NavMeshAgent radius - see below) to send a rerouted enemy's new NavMeshAgent destination.")]
    [SerializeField] private float enemyRerouteDistance = 2f;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private bool logObstacleHits = false;

    private BoxCollider obstacleCollider;

    // Reused every frame to avoid pushing the same actor twice if it has
    // multiple colliders (e.g. a trigger child + a main capsule collider).
    private readonly HashSet<Rigidbody> _processedRigidbodies = new HashSet<Rigidbody>();
    private readonly HashSet<NavMeshAgent> _processedAgents = new HashSet<NavMeshAgent>();

    private Collider[] _overlapBuffer = new Collider[32];

    private void Awake()
    {
        obstacleCollider = GetComponent<BoxCollider>();

        // Make sure this collider never physically blocks anything on its own --
        // it's a reference volume only, this script handles the actual blocking.
        if (!obstacleCollider.isTrigger)
        {
            Debug.LogWarning($"[{nameof(SolidObstacle)}] BoxCollider on {gameObject.name} should have " +
                              "'Is Trigger' checked. Enabling it automatically.");
            obstacleCollider.isTrigger = true;
        }
    }

    private void FixedUpdate()
    {
        Vector3 worldCenter = transform.TransformPoint(obstacleCollider.center);
        Vector3 worldHalfExtents = Vector3.Scale(obstacleCollider.size, transform.lossyScale) * 0.5f;

        int hitCount = Physics.OverlapBoxNonAlloc(
            worldCenter,
            worldHalfExtents,
            _overlapBuffer,
            transform.rotation,
            affectedLayers,
            QueryTriggerInteraction.Collide);

        if (hitCount == _overlapBuffer.Length)
        {
            // Buffer was full - grow it so we don't silently miss actors next frame.
            _overlapBuffer = new Collider[_overlapBuffer.Length * 2];
        }

        _processedRigidbodies.Clear();
        _processedAgents.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _overlapBuffer[i];
            if (hit == null || hit == obstacleCollider) continue;

            // IMPORTANT: check for a NavMeshAgent FIRST, regardless of whether
            // the actor also has a Rigidbody. Enemy prefabs often carry a
            // (frequently kinematic) Rigidbody just so trigger callbacks fire
            // correctly elsewhere in the project - if that's the case,
            // hit.attachedRigidbody would be non-null and the OLD code below
            // would route the enemy through TryPushRigidbody, which has no
            // idea about GoingDown at all. That let enemies get shoved/rerouted
            // even while GoingDown was true. Checking NavMeshAgent first and
            // filtering on GoingDown here means an enemy is NEVER pushed via
            // the rigidbody path either.
            NavMeshAgent agent = hit.GetComponentInParent<NavMeshAgent>();
            if (agent != null)
            {
                if (!_processedAgents.Add(agent)) continue; // already handled this frame

                InfluenzaFSM enemyFsm = agent.GetComponent<InfluenzaFSM>();
                if (enemyFsm != null && enemyFsm.GoingDown)
                {
                    // Still doing its initial "going down" move - leave it
                    // completely alone, don't push/reroute it at all.
                    if (logObstacleHits)
                    {
                        Debug.Log($"[{nameof(SolidObstacle)}] Skipped '{agent.name}' - GoingDown is true.");
                    }
                    continue;
                }

                TryPushAgent(agent, enemyFsm);
                continue;
            }

            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null)
            {
                if (!_processedRigidbodies.Add(rb)) continue; // already handled this frame
                TryPushRigidbody(rb);
            }
        }
    }

    private void TryPushRigidbody(Rigidbody rb)
    {
        // Rigidbody actors keep the point-based check as-is (actorRadius: 0)
        // - unlike the NavMeshAgent path below, the player's Rigidbody has no
        // single equivalent "radius" property to pull from generically here,
        // and this path isn't the one that was failing.
        if (!TryComputeEscapePoint(rb.position, surfacePadding, 0f, out Vector3 newPos, out Vector3 pushDir))
            return;

        rb.position = newPos;

        // Handle velocity: either zero only the inward component along the push
        // axis (lets the actor keep sliding along the wall), or kill it entirely.
        Vector3 vel = rb.linearVelocity; // Unity 2023.3+/6: linearVelocity. Older versions: use rb.velocity instead.

        if (onlyAffectPushAxisVelocity)
        {
            float inward = Vector3.Dot(vel, pushDir);
            if (inward < 0f)
            {
                vel -= pushDir * inward; // strip only the component driving them back into the box
            }
        }
        else
        {
            vel = Vector3.zero;
        }

        // Add a little outward kick so the pushback reads as a bounce, not a clip.
        vel += pushDir * pushBackForce;

        rb.linearVelocity = vel;

        if (logObstacleHits)
        {
            Debug.Log($"[{nameof(SolidObstacle)}] Pushed rigidbody actor '{rb.name}' back along {pushDir} to {newPos}.");
        }
    }

    // enemyFsm is passed in (rather than re-fetched) so we can notify it after
    // a successful reroute; may be null if the agent has no InfluenzaFSM.
    private void TryPushAgent(NavMeshAgent agent, InfluenzaFSM enemyFsm)
    {
        if (!agent.isOnNavMesh) return;

        // GoingDown is already filtered out by the caller (FixedUpdate) before
        // this method is ever invoked, so no need to check it again here.

        // Unlike the player, enemies aren't teleported - we just reroute their
        // NavMeshAgent toward a point outside the obstacle. Their own FSM
        // Update() is still what actually drives them there.
        //
        // BUG THIS FIXES: this used to call TryComputeEscapePoint with an
        // actorRadius of 0, i.e. it only checked whether the agent's single
        // root transform point was inside the box. But the detection that
        // found this agent in the first place (Physics.OverlapBoxNonAlloc,
        // up in FixedUpdate) is a real volume-vs-volume overlap test against
        // the agent's actual Collider - which has some physical radius. That
        // meant an agent could clearly be touching/overlapping the wall (so
        // it gets found and reaches this method, GoingDown false and all)
        // while its root transform.position was STILL just outside the box's
        // point-bounds - especially with a thin wall or any agent with real
        // width. TryComputeEscapePoint would then return false, and this
        // whole method would silently do nothing: no SetDestination, no
        // NotifyObstacleReroute call - which looked exactly like "the push
        // isn't pushing the enemy" even though GoingDown was correctly false.
        //
        // Fix: inflate the inside-check (and the resulting escape point) by
        // the agent's own NavMeshAgent.radius, so this point-based test lines
        // up with the volume-based overlap that triggered it.
        if (!TryComputeEscapePoint(agent.transform.position, enemyRerouteDistance, agent.radius, out Vector3 escapePoint, out Vector3 pushDir))
            return;

        if (NavMesh.SamplePosition(escapePoint, out NavMeshHit navHit, enemyRerouteDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);

            // Tell the FSM to hold off re-issuing its own destination for a
            // short window. Without this, InfluenzaFSM.UpdateChase() calls
            // _agent.SetDestination(target.position) every single Update(),
            // which runs AFTER this FixedUpdate and instantly overwrites the
            // reroute we just set - so the enemy never actually clears the
            // wall while chasing.
            enemyFsm?.NotifyObstacleReroute();
        }

        if (logObstacleHits)
        {
            Debug.Log($"[{nameof(SolidObstacle)}] Rerouted NavMeshAgent actor '{agent.name}' away from obstacle along {pushDir}.");
        }
    }

    // Shared inside-check + nearest-face escape-point math. Works in the box's
    // LOCAL space so rotation is respected exactly -- a world-axis-aligned
    // bounds check balloons out around a rotated collider, which causes
    // false "inside" hits before the actor actually touches the rotated box.
    //
    // extraDistance is how far past the surface the escape point should sit
    // (a tiny padding for the player's instant pushback, a larger distance
    // for an enemy's rerouted destination).
    //
    // actorRadius is the actor's own physical radius (0 if not applicable).
    // The obstacle's half-size is inflated by this amount before doing the
    // inside-check and picking the nearest face, so a wide actor is treated
    // as "inside" as soon as its BODY would overlap the box - matching what
    // the volume-based Physics.OverlapBoxNonAlloc call already detected -
    // rather than only once its single root point crosses the boundary.
    // The same inflated half-size is then used (plus extraDistance) to place
    // the escape point, so the actor's edge - not just its pivot - actually
    // clears the surface.
    private bool TryComputeEscapePoint(Vector3 worldPos, float extraDistance, float actorRadius, out Vector3 newWorldPos, out Vector3 pushDir)
    {
        newWorldPos = worldPos;
        pushDir = Vector3.zero;

        Vector3 localPos = transform.InverseTransformPoint(worldPos) - obstacleCollider.center;
        Vector3 halfSize = obstacleCollider.size * 0.5f + Vector3.one * Mathf.Max(0f, actorRadius);

        bool inside = Mathf.Abs(localPos.x) <= halfSize.x &&
                      Mathf.Abs(localPos.y) <= halfSize.y &&
                      Mathf.Abs(localPos.z) <= halfSize.z;

        if (!inside) return false;

        // Penetration-to-surface distance on each local axis (how far to the nearest face).
        float distX = halfSize.x - Mathf.Abs(localPos.x);
        float distY = halfSize.y - Mathf.Abs(localPos.y);
        float distZ = halfSize.z - Mathf.Abs(localPos.z);

        // Pick the axis with the smallest distance to a face -- that's the cheapest way out.
        int axis = 0; // 0 = x, 1 = y, 2 = z
        float minDist = distX;
        if (distY < minDist) { minDist = distY; axis = 1; }
        if (distZ < minDist) { minDist = distZ; axis = 2; }

        Vector3 pushDirLocal = Vector3.zero;
        Vector3 newLocalPos = localPos;

        switch (axis)
        {
            case 0:
                pushDirLocal = new Vector3(Mathf.Sign(localPos.x == 0f ? 1f : localPos.x), 0f, 0f);
                newLocalPos.x = pushDirLocal.x * (halfSize.x + extraDistance);
                break;
            case 1:
                pushDirLocal = new Vector3(0f, Mathf.Sign(localPos.y == 0f ? 1f : localPos.y), 0f);
                newLocalPos.y = pushDirLocal.y * (halfSize.y + extraDistance);
                break;
            case 2:
                pushDirLocal = new Vector3(0f, 0f, Mathf.Sign(localPos.z == 0f ? 1f : localPos.z));
                newLocalPos.z = pushDirLocal.z * (halfSize.z + extraDistance);
                break;
        }

        newWorldPos = transform.TransformPoint(newLocalPos + obstacleCollider.center);
        pushDir = transform.TransformDirection(pushDirLocal).normalized;

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}