using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// EnemyFSM — No NavMesh, pure transform movement.
///
/// PATROL  — follows spline forward, scans for Attackables
/// CHASE   — moves directly toward target
/// ATTACK  — in range, damages target on cooldown
/// FLEE    — took player damage, follows spline, then picks different target
/// DEAD    — stops, reports mission
/// </summary>
public class EnemyFSM : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int attackDamage = 10;
    public int currentHealth;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float fleeSpeed = 5f;
    [SerializeField] private float stoppingDistance = 1.2f;

    [Header("Spline Guide")]
    public SplineContainer splineContainer;
    [SerializeField] private float splineLookahead = 2f;
    [SerializeField] private float heightFollowSpeed = 50f;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 8f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Flee")]
    [SerializeField] private float fleeDuration = 3f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Targets")]
    [SerializeField] private List<Attackable> attackableTargets;

    [Header("Mission Submission")]
    public MissionSubmissionManager missionManager;
    public int missionIndex = 0;

    // ── FSM ───────────────────────────────────────────────────────────────────
    private enum State { Patrol, Chase, Attack, Flee, Dead }
    private State _state;

    // ── Private ───────────────────────────────────────────────────────────────
    private Attackable _currentTarget;
    private Attackable _lastTarget;
    private float _attackTimer;
    private bool _isDead;
    private float _splineT;
    private float _splineLength;
    private float _fleeTimer;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        currentHealth = maxHealth;

        if (splineContainer != null)
        {
            _splineLength = splineContainer.CalculateLength();
            SnapSplineTToSelf();
        }

        _state = State.Patrol;
    }

    void Update()
    {
        if (_isDead) return;

        if (_attackTimer > 0f)
            _attackTimer -= Time.deltaTime;

        switch (_state)
        {
            case State.Patrol: HandlePatrol(); break;
            case State.Chase: HandleChase(); break;
            case State.Attack: HandleAttack(); break;
            case State.Flee: HandleFlee(); break;
        }

        ApplySplineHeight();
    }

    // ── PATROL ────────────────────────────────────────────────────────────────
    void HandlePatrol()
    {
        if (splineContainer == null) return;

        _splineT += (patrolSpeed / _splineLength) * Time.deltaTime;
        if (_splineT >= 1f) _splineT -= 1f;

        float lookaheadT = Mathf.Repeat(_splineT + splineLookahead / _splineLength, 1f);
        Vector3 dest = GetSplinePositionWorld(lookaheadT);
        MoveToward(dest, patrolSpeed);

        Attackable found = ScanForTarget(null);
        if (found != null)
        {
            _currentTarget = found;
            _state = State.Chase;
        }
    }

    // ── CHASE ─────────────────────────────────────────────────────────────────
    void HandleChase()
    {
        if (_currentTarget == null || _currentTarget.IsDead)
        {
            _state = State.Patrol;
            return;
        }

        // Advance spline T so height keeps updating while chasing
        _splineT += (chaseSpeed / _splineLength) * Time.deltaTime;
        if (_splineT >= 1f) _splineT -= 1f;

        // Check distance using collider bounds if available, else transform distance
        float dist = GetDistanceToTarget(_currentTarget);

        if (dist <= stoppingDistance)
        {
            _state = State.Attack;
            return;
        }

        // Blend between spline forward and direct-to-target so enemy
        // stays near the path while still closing in on the target
        float lookaheadT = Mathf.Repeat(_splineT + splineLookahead / _splineLength, 1f);
        Vector3 splineDest = GetSplinePositionWorld(lookaheadT);
        Vector3 targetDest = _currentTarget.transform.position;

        // Closer to target = move more directly toward it
        float blend = Mathf.Clamp01(1f - dist / detectionRadius);
        Vector3 dest = Vector3.Lerp(splineDest, targetDest, blend);

        MoveToward(dest, chaseSpeed);
    }

    // ── ATTACK ────────────────────────────────────────────────────────────────
    void HandleAttack()
    {
        if (_currentTarget == null || _currentTarget.IsDead)
        {
            _state = State.Patrol;
            return;
        }

        FaceTarget(_currentTarget.transform.position);

        float dist = GetDistanceToTarget(_currentTarget);
        if (dist > stoppingDistance * 1.5f)
        {
            _state = State.Chase;
            return;
        }

        if (_attackTimer <= 0f)
        {
            _attackTimer = attackCooldown;
            StartCoroutine(DoAttack());
        }
    }

    /// <summary>Distance to target using its collider bounds if available.</summary>
    float GetDistanceToTarget(Attackable target)
    {
        Collider col = target.GetComponent<Collider>();
        if (col != null)
            return Vector3.Distance(transform.position, col.ClosestPoint(transform.position));
        return Vector3.Distance(transform.position, target.transform.position);
    }

    IEnumerator DoAttack()
    {
        yield return new WaitForSeconds(0.15f);
        if (_currentTarget != null && !_currentTarget.IsDead)
            _currentTarget.TakeDamage(attackDamage);
    }

    // ── FLEE ──────────────────────────────────────────────────────────────────
    void HandleFlee()
    {
        _fleeTimer -= Time.deltaTime;

        if (splineContainer != null)
        {
            _splineT += (fleeSpeed / _splineLength) * Time.deltaTime;
            if (_splineT >= 1f) _splineT -= 1f;

            float lookaheadT = Mathf.Repeat(_splineT + splineLookahead / _splineLength, 1f);
            Vector3 dest = GetSplinePositionWorld(lookaheadT);
            MoveToward(dest, fleeSpeed);
        }

        if (_fleeTimer <= 0f)
        {
            Attackable next = ScanForTarget(_lastTarget);
            if (next != null)
            {
                _currentTarget = next;
                _state = State.Chase;
            }
            else
            {
                _state = State.Patrol;
            }
        }
    }

    // ── TakeDamage (called by player) ─────────────────────────────────────────
    public void TakeDamage(int dmg)
    {
        if (_isDead) return;

        currentHealth -= dmg;
        if (currentHealth <= 0) { Die(); return; }

        StopAllCoroutines();
        _attackTimer = attackCooldown;
        _lastTarget = _currentTarget;
        _currentTarget = null;
        _fleeTimer = fleeDuration;
        SnapSplineTToSelf();
        _state = State.Flee;
    }

    // ── Height from spline ────────────────────────────────────────────────────
    void ApplySplineHeight()
    {
        if (splineContainer == null) return;

        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            splineContainer.transform.InverseTransformPoint(transform.position),
            out _, out float heightT);

        float targetY = GetSplinePositionWorld(heightT).y;
        Vector3 pos = transform.position;
        pos.y = Mathf.MoveTowards(pos.y, targetY, heightFollowSpeed * Time.deltaTime);
        transform.position = pos;
    }

    // ── Movement Helpers ──────────────────────────────────────────────────────
    void MoveToward(Vector3 destination, float speed)
    {
        Vector3 dir = destination - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.position += dir.normalized * speed * Time.deltaTime;
        FaceTarget(destination);
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                                                 rotationSpeed * Time.deltaTime);
    }

    // ── Scan for target ───────────────────────────────────────────────────────
    Attackable ScanForTarget(Attackable exclude)
    {
        Attackable best = null;
        float bestDist = float.MaxValue;

        foreach (var a in attackableTargets)
        {
            if (a == null || a.IsDead) continue;
            if (a == exclude) continue;

            float dist = Vector3.Distance(transform.position, a.transform.position);
            if (dist <= detectionRadius && dist < bestDist)
            {
                bestDist = dist;
                best = a;
            }
        }

        return best;
    }

    // ── Spline Helpers ────────────────────────────────────────────────────────
    void SnapSplineTToSelf()
    {
        if (splineContainer == null) return;
        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            splineContainer.transform.InverseTransformPoint(transform.position),
            out _, out _splineT);
    }

    Vector3 GetSplinePositionWorld(float t)
    {
        Vector3 local = SplineUtility.EvaluatePosition(splineContainer.Spline, t);
        return splineContainer.transform.TransformPoint(local);
    }

    // ── Die ───────────────────────────────────────────────────────────────────
    void Die()
    {
        _isDead = true;
        _state = State.Dead;
        StopAllCoroutines();

        if (missionManager != null)
        {
            missionManager.CompleteMissionByIndex(missionIndex);
            Debug.Log("[EnemyFSM] Died — Mission " + missionIndex + " completed.");
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        Vector3 pos = transform.position;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawWireSphere(pos, detectionRadius);

        if (splineContainer != null && Application.isPlaying)
        {
            Vector3 splinePos = GetSplinePositionWorld(_splineT);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(splinePos, 0.2f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos, splinePos);
        }

        if (_currentTarget != null && !_currentTarget.IsDead)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos + Vector3.up * 0.5f, _currentTarget.transform.position + Vector3.up);
            Gizmos.DrawSphere(_currentTarget.transform.position + Vector3.up, 0.4f);
        }

        if (attackableTargets != null)
        {
            foreach (var a in attackableTargets)
            {
                if (a == null || a.IsDead) continue;
                Gizmos.color = (a == _currentTarget) ? Color.red : Color.green;
                Gizmos.DrawSphere(a.transform.position + Vector3.up, 0.3f);
            }
        }

#if UNITY_EDITOR
        string label = $"State: {_state}"
                     + $"\nHP: {currentHealth}/{maxHealth}"
                     + $"\nTarget: {(_currentTarget != null ? _currentTarget.name : "none")}"
                     + $"\nLast: {(_lastTarget != null ? _lastTarget.name : "none")}"
                     + $"\nSplineT: {_splineT:F2}";
        UnityEditor.Handles.Label(pos + Vector3.up * 3f, label);
#endif
    }
}