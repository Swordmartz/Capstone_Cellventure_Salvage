using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyFSM : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    public int currentHealth;
    [SerializeField] private int attackDamage = 10;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float stoppingDistance = 1.2f;

    [Header("Speed Boost (on hit)")]
    [SerializeField] private float speedBoostMultiplier = 1.8f;
    [SerializeField] private float speedBoostDuration = 2f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Targets")]
    [SerializeField] private List<Attackable> attackableTargets;

    [Header("Teleports")]
    [SerializeField] private List<Transform> teleportPoints;

    [Header("Wall Check")]
    [SerializeField] private LayerMask wallMask;

    [Header("Axis Threshold (Teleport)")]
    [SerializeField] private float axisThreshold = 2f;

    [Header("Mission Submission")]
    public MissionSubmissionManager missionManager;
    public int missionIndex = 0;

    private enum State { MoveToTarget, Attack, MoveToTeleport, Dead }

    private State state;
    private NavMeshAgent agent;
    private Attackable currentTarget;

    private int occupiedTeleportIndex = -1;
    private int lastOccupiedTeleportIndex = -1;
    private int walkingToTeleportIndex = -1;

    private float attackTimer;
    private bool isDead;
    private Coroutine speedRoutine;

    // ─────────────────────────────
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.stoppingDistance = stoppingDistance;
        currentHealth = maxHealth;
        state = State.MoveToTarget;
        PickRandomTarget();
    }

    private void Update()
    {
        if (isDead) return;

        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        switch (state)
        {
            case State.MoveToTarget: HandleMoveToTarget(); break;
            case State.Attack: HandleAttack(); break;
            case State.MoveToTeleport: HandleMoveToTeleport(); break;
        }
    }

    void PickRandomTarget()
    {
        Attackable nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var a in attackableTargets)
        {
            if (a == null || a.IsDead) continue;

            Vector2 enemyXZ = new Vector2(transform.position.x, transform.position.z);
            Vector2 targetXZ = new Vector2(a.transform.position.x, a.transform.position.z);
            float d = Vector2.Distance(enemyXZ, targetXZ);
            if (d < nearestDist) { nearestDist = d; nearest = a; }
        }

        if (nearest == null)
        {
            Debug.Log("[EnemyFSM] No alive targets found.");
            return;
        }

        Debug.Log("[EnemyFSM] Targeting " + nearest.name);
        currentTarget = nearest;
        agent.isStopped = false;
        agent.SetDestination(currentTarget.transform.position);
        state = State.MoveToTarget;
    }

    void HandleMoveToTarget()
    {
        if (currentTarget == null || currentTarget.IsDead) { PickTeleport(); return; }
        if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
            state = State.Attack;
    }

    void HandleAttack()
    {
        if (currentTarget == null || currentTarget.IsDead) { PickTeleport(); return; }
        if (attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            StartCoroutine(DoAttack());
        }
    }

    IEnumerator DoAttack()
    {
        if (currentTarget != null && !currentTarget.IsDead)
        {
            currentTarget.TakeDamage(attackDamage);
            if (currentTarget.IsDead) { PickTeleport(); yield break; }
        }
        yield return new WaitForSeconds(0.3f);
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        currentHealth -= dmg;
        if (currentHealth <= 0) { Die(); return; }

        StopAllCoroutines();
        attackTimer = attackCooldown;
        PickTeleport();
        ApplySpeedBoost();
    }

    void DoInstantWarp()
    {
        int index = PickTeleportIndex(occupiedTeleportIndex);

        Debug.Log($"[EnemyFSM] DoInstantWarp — occupiedIndex={occupiedTeleportIndex}, chosen={index}");

        if (index < 0)
        {
            Debug.LogWarning("[EnemyFSM] DoInstantWarp — no valid teleport found!");
            return;
        }

        lastOccupiedTeleportIndex = -1;
        occupiedTeleportIndex = index;
        walkingToTeleportIndex = -1;

        Debug.Log($"[EnemyFSM] Warping to index={index} name={teleportPoints[index].name}");

        agent.Warp(teleportPoints[index].position);
        state = State.MoveToTarget;
        PickRandomTarget();
    }

    void PickTeleport()
    {
        int index = PickTeleportIndex(occupiedTeleportIndex);

        Debug.Log($"[EnemyFSM] PickTeleport — occupiedIndex={occupiedTeleportIndex}, chosen={index}");

        if (index < 0)
        {
            Debug.LogWarning("[EnemyFSM] PickTeleport — no valid point, chasing target.");
            state = State.MoveToTarget;
            PickRandomTarget();
            return;
        }

        lastOccupiedTeleportIndex = occupiedTeleportIndex;
        walkingToTeleportIndex = index;

        Debug.Log($"[EnemyFSM] Walking to trigger index={index} name={teleportPoints[index].name}");

        agent.isStopped = false;
        agent.SetDestination(teleportPoints[index].position);
        state = State.MoveToTeleport;
    }

    void HandleMoveToTeleport()
    {
        if (walkingToTeleportIndex < 0) { PickRandomTarget(); return; }

        if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
        {
            int triggerIndex = walkingToTeleportIndex;
            walkingToTeleportIndex = -1;

            Debug.Log($"[EnemyFSM] Reached trigger={triggerIndex}. Excluding trigger={triggerIndex} AND lastOccupied={lastOccupiedTeleportIndex}");

            int warpIndex = PickTeleportIndex(triggerIndex, lastOccupiedTeleportIndex);

            if (warpIndex < 0)
                warpIndex = PickTeleportIndex(triggerIndex);

            lastOccupiedTeleportIndex = -1;

            if (warpIndex < 0)
            {
                Debug.LogWarning("[EnemyFSM] HandleMoveToTeleport — no valid warp destination!");
                state = State.MoveToTarget;
                PickRandomTarget();
                return;
            }

            Debug.Log($"[EnemyFSM] Warping to index={warpIndex} name={teleportPoints[warpIndex].name}");

            agent.Warp(teleportPoints[warpIndex].position);
            occupiedTeleportIndex = warpIndex;
            state = State.MoveToTarget;
            PickRandomTarget();
        }
    }

    int PickTeleportIndex(params int[] excludeList)
    {
        List<int> candidates = new List<int>();

        for (int i = 0; i < teleportPoints.Count; i++)
        {
            if (teleportPoints[i] == null) continue;

            bool excluded = false;
            foreach (int ex in excludeList) if (i == ex) { excluded = true; break; }
            if (excluded) continue;

            bool sameX = Mathf.Abs(teleportPoints[i].position.x - transform.position.x) <= axisThreshold;
            bool sameZ = Mathf.Abs(teleportPoints[i].position.z - transform.position.z) <= axisThreshold;

            if (sameX || sameZ)
                candidates.Add(i);
        }

        Debug.Log($"[EnemyFSM] PickTeleportIndex — Pass1 axis candidates: {candidates.Count}, excluding: {string.Join(",", excludeList)}");

        if (candidates.Count == 0)
        {
            for (int i = 0; i < teleportPoints.Count; i++)
            {
                if (teleportPoints[i] == null) continue;

                bool excluded = false;
                foreach (int ex in excludeList) if (i == ex) { excluded = true; break; }
                if (excluded) continue;

                candidates.Add(i);
            }
            Debug.Log($"[EnemyFSM] PickTeleportIndex — Pass2 fallback candidates: {candidates.Count}");
        }

        if (candidates.Count == 0) return -1;

        return candidates[Random.Range(0, candidates.Count)];
    }

    void ApplySpeedBoost()
    {
        if (speedRoutine != null) StopCoroutine(speedRoutine);
        speedRoutine = StartCoroutine(SpeedBoostRoutine());
    }

    IEnumerator SpeedBoostRoutine()
    {
        agent.speed = moveSpeed * speedBoostMultiplier;
        yield return new WaitForSeconds(speedBoostDuration);
        agent.speed = moveSpeed;
    }

    void Die()
    {
        isDead = true;
        state = State.Dead;
        StopAllCoroutines();
        agent.isStopped = true;
        agent.ResetPath();

        if (missionManager != null)
        {
            missionManager.CompleteMissionByIndex(missionIndex);
            Debug.Log("[EnemyFSM] Enemy died — Mission " + missionIndex + " completed.");
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 pos = transform.position;

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.2f);
        Gizmos.DrawCube(pos, new Vector3(axisThreshold * 2f, 0.1f, 200f));
        Gizmos.DrawCube(pos, new Vector3(200f, 0.1f, axisThreshold * 2f));

        if (teleportPoints != null)
        {
            for (int i = 0; i < teleportPoints.Count; i++)
            {
                if (teleportPoints[i] == null) continue;

                bool sameX = Mathf.Abs(teleportPoints[i].position.x - pos.x) <= axisThreshold;
                bool sameZ = Mathf.Abs(teleportPoints[i].position.z - pos.z) <= axisThreshold;
                bool isOccupied = (i == occupiedTeleportIndex);
                bool isWalkingTo = (i == walkingToTeleportIndex);

                if (isOccupied)
                    Gizmos.color = Color.red;
                else if (isWalkingTo)
                    Gizmos.color = Color.yellow;
                else if (sameX || sameZ)
                    Gizmos.color = Color.cyan;
                else
                    Gizmos.color = new Color(1f, 1f, 1f, 0.3f);

                Gizmos.DrawSphere(teleportPoints[i].position + Vector3.up * 0.5f, 0.4f);

                if ((sameX || sameZ) && !isOccupied)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(pos + Vector3.up * 0.5f, teleportPoints[i].position + Vector3.up * 0.5f);
                }
            }
        }

        if (attackableTargets != null)
        {
            foreach (var a in attackableTargets)
            {
                if (a == null || a.IsDead) continue;

                bool isCurrentTarget = (a == currentTarget);

                Gizmos.color = isCurrentTarget ? Color.red : Color.green;
                Gizmos.DrawSphere(a.transform.position + Vector3.up * 1f, 0.5f);

                if (isCurrentTarget)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(pos + Vector3.up * 0.5f, a.transform.position + Vector3.up * 1f);
                }
            }
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(pos + Vector3.up * 2.5f, new Vector3(0.1f, 0.1f, 0.1f));

#if UNITY_EDITOR
        string label = "State: " + state
                     + "\nOccupied TP: " + occupiedTeleportIndex
                     + "\nLast Occupied TP: " + lastOccupiedTeleportIndex
                     + "\nWalking To TP: " + walkingToTeleportIndex;
        UnityEditor.Handles.Label(pos + Vector3.up * 3f, label);
#endif
    }
}