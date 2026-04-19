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

    private enum State
    {
        MoveToTarget,
        Attack,
        MoveToTeleport,
        Dead
    }

    private State state;

    private NavMeshAgent agent;
    private Attackable currentTarget;
    private Transform currentTeleport;

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

    // ─────────────────────────────
    // RANDOM TARGET
    // ─────────────────────────────
    void PickRandomTarget()
    {
        List<Attackable> valid = new List<Attackable>();

        foreach (var a in attackableTargets)
        {
            if (a == null || a.IsDead) continue;
            valid.Add(a);
        }

        if (valid.Count == 0)
        {
            PickTeleport();
            return;
        }

        currentTarget = valid[Random.Range(0, valid.Count)];

        agent.isStopped = false;
        agent.SetDestination(currentTarget.transform.position);
    }

    void HandleMoveToTarget()
    {
        if (currentTarget == null || currentTarget.IsDead)
        {
            PickRandomTarget();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
        {
            state = State.Attack;
        }
    }

    void HandleAttack()
    {
        if (currentTarget == null || currentTarget.IsDead)
        {
            PickRandomTarget();
            return;
        }

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

            if (currentTarget.IsDead)
            {
                PickRandomTarget();
                yield break;
            }
        }

        yield return new WaitForSeconds(0.3f);
    }

    // ─────────────────────────────
    // 💥 TAKE DAMAGE → TELEPORT + SPEED BOOST
    // ─────────────────────────────
    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHealth -= dmg;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        PickTeleport();
        ApplySpeedBoost();
    }

    // ─────────────────────────────
    // 🔥 SPEED BOOST SYSTEM
    // ─────────────────────────────
    void ApplySpeedBoost()
    {
        if (speedRoutine != null)
            StopCoroutine(speedRoutine);

        speedRoutine = StartCoroutine(SpeedBoostRoutine());
    }

    IEnumerator SpeedBoostRoutine()
    {
        agent.speed = moveSpeed * speedBoostMultiplier;

        yield return new WaitForSeconds(speedBoostDuration);

        agent.speed = moveSpeed;
    }

    // ─────────────────────────────
    // TELEPORT
    // ─────────────────────────────
    void PickTeleport()
    {
        List<Transform> valid = new List<Transform>();

        foreach (Transform tp in teleportPoints)
        {
            if (tp == null) continue;

            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 target = tp.position + Vector3.up * 1f;

            Vector3 dir = (target - origin).normalized;
            float dist = Vector3.Distance(origin, target);

            if (Physics.Raycast(origin, dir, dist, wallMask))
                continue;

            valid.Add(tp);
        }

        if (valid.Count == 0)
        {
            PickRandomTarget();
            return;
        }

        currentTeleport = valid[Random.Range(0, valid.Count)];

        agent.isStopped = false;
        agent.SetDestination(currentTeleport.position);

        state = State.MoveToTeleport;
    }

    void HandleMoveToTeleport()
    {
        if (currentTeleport == null)
        {
            PickRandomTarget();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
        {
            agent.Warp(currentTeleport.position);

            state = State.MoveToTarget;
            PickRandomTarget();
        }
    }

    void Die()
    {
        isDead = true;
        state = State.Dead;

        StopAllCoroutines();
        agent.isStopped = true;
        agent.ResetPath();
    }
}