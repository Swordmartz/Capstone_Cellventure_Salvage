using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

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

    [Header("Mission Reference")]
    public AI_TestTD missionData;

    [Header("On Death")]
    public GameObject objectToActivateOnDeath;

    [Header("Hit Flash")]
    public SpriteRenderer spriteRenderer;
    public float flashDuration = 0.1f;
    public int flashCount = 2;

    [Header("HP Bar")]
    public UnityEngine.UI.Slider hpBarSlider;

    [Header("Freeze / Stagger")]
    [Tooltip("Read-only: time remaining (in seconds) that this enemy is frozen for.")]
    [SerializeField] private float freezeTimer = 0f;

    [Header("Slow / DoT (Super Attack)")]
    [Tooltip("Read-only: current movement speed multiplier. 1 = normal, 0.5 = half speed.")]
    [SerializeField] private float slowMultiplier = 1f;
    [Tooltip("Read-only: time remaining (in seconds) that the slow is active for.")]
    [SerializeField] private float slowTimer = 0f;

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

    public bool IsFrozen => freezeTimer > 0f;
    public bool IsSlowed => slowTimer > 0f;

    private Coroutine _dotRoutine;

    void Start()
    {
        currentHealth = maxHealth;

        if (splineContainer != null)
        {
            _splineLength = splineContainer.CalculateLength();
            SnapSplineTToSelf();
        }

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (hpBarSlider != null)
        {
            hpBarSlider.maxValue = maxHealth;
            hpBarSlider.value = currentHealth;
        }

        _state = State.Patrol;
    }

    void Update()
    {
        if (_isDead) return;

        // ── Freeze / Stagger ──
        // While frozen: no movement, no state transitions, no attack timer
        // countdown — completely stuck in place. Only the freeze timer itself
        // counts down.
        if (freezeTimer > 0f)
        {
            freezeTimer -= Time.deltaTime;
            return;
        }

        if (_attackTimer > 0f)
            _attackTimer -= Time.deltaTime;

        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
                slowMultiplier = 1f;
        }

        switch (_state)
        {
            case State.Patrol: HandlePatrol(); break;
            case State.Chase: HandleChase(); break;
            case State.Attack: HandleAttack(); break;
            case State.Flee: HandleFlee(); break;
        }

        ApplySplineHeight();
    }

    /// <summary>
    /// Freezes this enemy completely for the given duration. If already
    /// frozen, the timer is refreshed to the longer of its current remaining
    /// time or the new duration — so repeated hits (e.g. from a rapid attack)
    /// keep the enemy locked for as long as hits keep landing.
    /// </summary>
    public void Freeze(float duration)
    {
        if (_isDead) return;

        freezeTimer = Mathf.Max(freezeTimer, duration);
    }

    void HandlePatrol()
    {
        if (splineContainer == null) return;

        _splineT += (patrolSpeed * slowMultiplier / _splineLength) * Time.deltaTime;
        if (_splineT >= 1f) _splineT -= 1f;

        float lookaheadT = Mathf.Repeat(_splineT + splineLookahead / _splineLength, 1f);
        Vector3 dest = GetSplinePositionWorld(lookaheadT);
        MoveToward(dest, patrolSpeed * slowMultiplier);

        Attackable found = ScanForTarget(null);
        if (found != null)
        {
            _currentTarget = found;
            _state = State.Chase;
        }
    }

    void HandleChase()
    {
        if (_currentTarget == null || _currentTarget.IsDead)
        {
            _state = State.Patrol;
            return;
        }

        _splineT += (chaseSpeed * slowMultiplier / _splineLength) * Time.deltaTime;
        if (_splineT >= 1f) _splineT -= 1f;

        float dist = GetDistanceToTarget(_currentTarget);

        if (dist <= stoppingDistance)
        {
            _state = State.Attack;
            return;
        }

        float lookaheadT = Mathf.Repeat(_splineT + splineLookahead / _splineLength, 1f);
        Vector3 splineDest = GetSplinePositionWorld(lookaheadT);
        Vector3 targetDest = _currentTarget.transform.position;

        float blend = Mathf.Clamp01(1f - dist / detectionRadius);
        Vector3 dest = Vector3.Lerp(splineDest, targetDest, blend);

        MoveToward(dest, chaseSpeed * slowMultiplier);
    }

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

    void HandleFlee()
    {
        _fleeTimer -= Time.deltaTime;

        if (splineContainer != null)
        {
            _splineT += (fleeSpeed * slowMultiplier / _splineLength) * Time.deltaTime;
            if (_splineT >= 1f) _splineT -= 1f;

            float lookaheadT = Mathf.Repeat(_splineT + splineLookahead / _splineLength, 1f);
            Vector3 dest = GetSplinePositionWorld(lookaheadT);
            MoveToward(dest, fleeSpeed * slowMultiplier);
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

    [Header("Damage Floor")]
    [Tooltip("Normal damage (spear/rapid attack) will never reduce HP below this value. " +
             "Only a super attack calling SuperKill() can actually kill this enemy.")]
    [SerializeField] private int minHealthFloor = 10;

    public void TakeDamage(int dmg)
    {
        if (_isDead) return;

        currentHealth -= dmg;

        // Normal attacks can never bring this enemy below the floor, and can
        // never kill it outright — only SuperKill() can finish it off.
        if (currentHealth < minHealthFloor)
            currentHealth = minHealthFloor;

        if (hpBarSlider != null)
            hpBarSlider.value = currentHealth;

        StopCoroutine(nameof(FlashRed));
        StartCoroutine(FlashRed());

        _attackTimer = attackCooldown;
        _lastTarget = _currentTarget;
        _currentTarget = null;
        _fleeTimer = fleeDuration;
        SnapSplineTToSelf();
        _state = State.Flee;
    }

    /// <summary>
    /// The only way this enemy actually dies. Regular attacks (spear/rapid)
    /// only call TakeDamage, which floors at minHealthFloor and never kills.
    /// A super attack should call this directly to finish the enemy off,
    /// regardless of its current HP.
    /// </summary>
    public void SuperKill()
    {
        if (_isDead) return;

        currentHealth = 0;

        if (hpBarSlider != null)
            hpBarSlider.value = 0;

        Die();
    }

    /// <summary>
    /// Reduces movement speed by the given multiplier (e.g. 0.5 = half speed)
    /// for the given duration. Refreshes the duration if already slowed;
    /// does NOT stack multiple multipliers — the most recent call wins.
    /// </summary>
    public void ApplySlow(float multiplier, float duration)
    {
        if (_isDead) return;

        slowMultiplier = Mathf.Clamp01(multiplier);
        slowTimer = Mathf.Max(slowTimer, duration);
    }

    /// <summary>
    /// Applies a damage-over-time effect that bypasses minHealthFloor and can
    /// kill this enemy outright (calls SuperKill once total damage reduces
    /// HP to 0 or below). Used by the player's super attack.
    /// totalDamage is spread evenly across (duration / tickInterval) ticks.
    /// If this enemy is already affected by a DoT, the new call replaces it.
    /// </summary>
    public void ApplyDamageOverTime(int totalDamage, float duration, float tickInterval)
    {
        if (_isDead) return;

        if (_dotRoutine != null)
            StopCoroutine(_dotRoutine);

        _dotRoutine = StartCoroutine(DamageOverTimeRoutine(totalDamage, duration, tickInterval));
    }

    private IEnumerator DamageOverTimeRoutine(int totalDamage, float duration, float tickInterval)
    {
        int tickCount = Mathf.Max(1, Mathf.RoundToInt(duration / tickInterval));
        int damagePerTick = Mathf.Max(1, totalDamage / tickCount);

        for (int i = 0; i < tickCount; i++)
        {
            if (_isDead) yield break;

            currentHealth -= damagePerTick;

            if (hpBarSlider != null)
                hpBarSlider.value = Mathf.Max(currentHealth, 0);

            if (currentHealth <= 0)
            {
                SuperKill();
                yield break;
            }

            yield return new WaitForSeconds(tickInterval);
        }

        _dotRoutine = null;
    }

    private IEnumerator FlashRed()
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;

        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = Color.red;

            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashDuration;
                spriteRenderer.color = Color.Lerp(Color.red, originalColor, t);
                yield return null;
            }
        }

        spriteRenderer.color = originalColor;
    }

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

    void Die()
    {
        _isDead = true;
        _state = State.Dead;
        StopAllCoroutines();

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        if (hpBarSlider != null)
        {
            hpBarSlider.value = 0;
            hpBarSlider.gameObject.SetActive(false);
        }

        if (missionData != null && missionData.missionTimer != null)
        {
            missionData.EnemyDeathTime = Mathf.RoundToInt(missionData.missionTimer.GetCurrentTime());
            missionData.missionTimer.StopTimer();
        }

        if (objectToActivateOnDeath != null)
            objectToActivateOnDeath.SetActive(true);

        if (missionManager != null)
            missionManager.CompleteMissionByIndex(missionIndex);
    }

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
                     + $"\nSplineT: {_splineT:F2}"
                     + $"\nFrozen: {(IsFrozen ? freezeTimer.ToString("F2") : "no")}"
                     + $"\nSlowed: {(IsSlowed ? $"{slowMultiplier:F2}x for {slowTimer:F2}s" : "no")}";
        UnityEditor.Handles.Label(pos + Vector3.up * 3f, label);
#endif
    }
}