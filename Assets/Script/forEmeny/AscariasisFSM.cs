using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class EnemyPatrolFSM : MonoBehaviour
{
    private enum State { Patrol, MovingToNutrient, Eating, ReturningToPath, Dead }

    [Header("State (read-only, for debugging)")]
    [SerializeField] private State _state = State.Patrol;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    public int currentHealth;
    [Tooltip("Base movement speed. Used as a general reference stat; individual state speeds below can still be tuned separately.")]
    [SerializeField] private float speed = 3f;
    [Tooltip("Regular attacks (spear, rapid attack) cannot reduce HP below this value. " +
             "Only the super attack's damage-over-time can push HP below this floor and kill the enemy.")]
    [SerializeField] private int regularAttackDamageFloor = 10;

    public int MaxHealth => maxHealth;

    [Header("Health Bar UI")]
    [Tooltip("Optional. If assigned, this slider's value is kept in sync with currentHealth, " +
             "and its max value is set to maxHealth on Start.")]
    [SerializeField] private Slider healthBar;

    [Header("Spline Guide")]
    public SplineContainer splineContainer;
    [SerializeField] private float patrolSpeed = 2f;

    private float _splineT;
    private float _splineLength;

    [Header("Nutrient Detection")]
    public string nutrientTag = "Nutrients";
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private float detectionInterval = 0.5f;
    private float _detectionTimer;

    public enum NutrientSelectionMode { Nearest, Random }
    public NutrientSelectionMode selectionMode = NutrientSelectionMode.Nearest;

    [Header("Moving To Nutrient")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float reachDistance = 0.5f;

    [Header("Eating")]
    [Tooltip("How long (in seconds) the enemy stays put and eats after reaching a nutrient, before heading back to the spline.")]
    [SerializeField] private float eatDuration = 2f;
    private float _eatTimer;

    [Header("Returning To Path")]
    [SerializeField] private float returnSpeed = 4f;
    [SerializeField] private float returnReachDistance = 0.3f;
    [Tooltip("After returning to the spline, nutrient detection is paused for this many seconds before it can chase another one.")]
    [SerializeField] private float postReturnCooldown = 2f;
    private float _cooldownTimer;

    [Header("Animation")]
    [Tooltip("Optional. If assigned, the Animator's LastX/LastY float parameters are updated " +
             "with the current movement direction (X/Z plane). This does NOT rotate the transform - " +
             "facing/rotation is left entirely to your billboard script.")]
    [SerializeField] private Animator animator;

    private static readonly int LastXHash = Animator.StringToHash("LastX");
    private static readonly int LastYHash = Animator.StringToHash("LastY");

    [Header("Hit Flash")]
    [Tooltip("Optional. If assigned, this sprite briefly turns flashColor whenever the enemy takes damage. " +
             "If left empty, the script will try GetComponentInChildren<SpriteRenderer>() at Start.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;
    private Color _originalSpriteColor;
    private Coroutine _flashRoutine;

    [Header("On Death")]
    public GameObject objectToActivateOnDeath;

    [Header("Values For Star (WBC)")]
    [Tooltip("ValuesForStar component to report into. Incremented by 1 the moment this enemy dies.")]
    [SerializeField] private ValuesForStar valuesForStar;

    [Header("Slow / DoT (Super Attack)")]
    [Tooltip("Read-only: current movement speed multiplier. 1 = normal, 0.5 = half speed.")]
    [SerializeField] private float slowMultiplier = 1f;
    [Tooltip("Read-only: time remaining (in seconds) that the slow is active for.")]

    [Header("Freeze / Stagger")]
    [SerializeField] private float freezeTimer = 0f;

    public bool IsFrozen => freezeTimer > 0f;
    [SerializeField] private float slowTimer = 0f;

    public bool IsSlowed => slowTimer > 0f;

    private Coroutine _dotRoutine;

    private GameObject _targetNutrient;
    private bool _isDead;

    public bool IsDead => _isDead;

    void Start()
    {
        currentHealth = maxHealth;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            _originalSpriteColor = spriteRenderer.color;

        if (healthBar != null)
        {
            healthBar.minValue = 0;
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        if (splineContainer != null)
        {
            _splineLength = splineContainer.CalculateLength();
            SnapSplineTToSelf();
        }
    }

    void Update()
    {
        if (healthBar != null)
            healthBar.value = currentHealth;

        if (_isDead) return;

        // While frozen: no movement, no state transitions — completely
        // stuck in place. Only the freeze timer itself counts down.
        if (freezeTimer > 0f)
        {
            freezeTimer -= Time.deltaTime;
            return;
        }

        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
                slowMultiplier = 1f;
        }

        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        switch (_state)
        {
            case State.Patrol: HandlePatrol(); break;
            case State.MovingToNutrient: HandleMovingToNutrient(); break;
            case State.Eating: HandleEating(); break;
            case State.ReturningToPath: HandleReturningToPath(); break;
        }
    }

    // =========================================================
    // HEALTH
    // =========================================================

    /// <summary>
    /// Applies damage from regular attacks (spear melee, rapid attack burst).
    /// This damage CANNOT reduce HP below regularAttackDamageFloor (default 10) —
    /// it will never kill the enemy on its own. Only ApplyDamageOverTime
    /// (the super attack's DoT) can finish the enemy off.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (_isDead) return;

        // Already at or below the floor — regular attacks can't do anything more.
        if (currentHealth <= regularAttackDamageFloor) return;

        currentHealth -= amount;

        // Clamp back up to the floor instead of letting it go lower / killing.
        if (currentHealth < regularAttackDamageFloor)
            currentHealth = regularAttackDamageFloor;

        FlashHit();
    }

    public void Heal(int amount)
    {
        if (_isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
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
    /// Applies a damage-over-time effect that can kill this enemy outright
    /// (calls Die once total damage reduces HP to 0 or below). Used by the
    /// player's super attack. Unlike TakeDamage, this is NOT limited by
    /// regularAttackDamageFloor — it can reduce HP all the way to 0.
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
            FlashHit();

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
                yield break;
            }

            yield return new WaitForSeconds(tickInterval);
        }

        _dotRoutine = null;
    }

    /// <summary>
    /// Briefly tints spriteRenderer flashColor, then restores the original color.
    /// Safe to call repeatedly - each call restarts the flash from full color.
    /// </summary>
    void FlashHit()
    {
        if (spriteRenderer == null) return;

        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);

        _flashRoutine = StartCoroutine(FlashHitRoutine());
    }

    private IEnumerator FlashHitRoutine()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = _originalSpriteColor;
        _flashRoutine = null;
    }

    void Die()
    {
        _isDead = true;
        _state = State.Dead;
        StopAllCoroutines();

        if (animator != null)
            animator.SetBool("Death", true);

        if (objectToActivateOnDeath != null)
            objectToActivateOnDeath.SetActive(true);

        // Report this kill to ValuesForStar's WBC field (EnemyKilled). This is
        // the single place currentHealth reaching 0 / _state becoming Dead is
        // handled, so no separate "if HP == 0 && state == Dead" check is needed
        // elsewhere - Die() only ever runs once per enemy (guarded by the
        // _isDead check at the top of TakeDamage/ApplyDamageOverTime/etc.,
        // and DamageOverTimeRoutine only calls Die() a single time before
        // returning).
        if (valuesForStar != null)
            valuesForStar.ReportEnemyKilled();
        else
            Debug.LogWarning($"[EnemyPatrolFSM] {name}: died but no valuesForStar is assigned - kill was not reported.");

        // Add death VFX/animation/disable logic here as needed.
    }

    // =========================================================
    // PATROL
    // =========================================================
    void HandlePatrol()
    {
        if (splineContainer == null) return;

        _splineT += (patrolSpeed * slowMultiplier / _splineLength) * Time.deltaTime;
        if (_splineT >= 1f) _splineT -= 1f;

        Vector3 dest = GetSplinePositionWorld(_splineT);
        MoveToward(dest, patrolSpeed * slowMultiplier);

        _detectionTimer += Time.deltaTime;
        if (_detectionTimer >= detectionInterval)
        {
            _detectionTimer = 0f;

            if (_cooldownTimer <= 0f)
            {
                GameObject found = DetectNutrient();
                if (found != null)
                {
                    _targetNutrient = found;
                    _state = State.MovingToNutrient;
                }
            }
        }
    }

    // =========================================================
    // DETECTION
    // =========================================================
    GameObject DetectNutrient()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        List<GameObject> candidates = new List<GameObject>();

        foreach (var hit in hits)
        {
            if (hit.CompareTag(nutrientTag) && hit.gameObject.activeInHierarchy)
                candidates.Add(hit.gameObject);
        }

        if (candidates.Count == 0) return null;

        if (selectionMode == NutrientSelectionMode.Random)
            return candidates[Random.Range(0, candidates.Count)];

        GameObject nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var c in candidates)
        {
            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = c;
            }
        }
        return nearest;
    }

    // =========================================================
    // MOVING TO NUTRIENT
    // =========================================================
    // Chases the target's live position every frame (re-reads its transform
    // each call, so it tracks a moving nutrient too) until within
    // reachDistance, then deactivates it immediately and heads back to
    // the spline — no consume/wait state in between.
    void HandleMovingToNutrient()
    {
        if (_targetNutrient == null || !_targetNutrient.activeInHierarchy)
        {
            _targetNutrient = null;
            _state = State.ReturningToPath;
            return;
        }

        Vector3 targetPos = _targetNutrient.transform.position;
        MoveToward(targetPos, chaseSpeed * slowMultiplier, restrictY: false);

        if (Vector3.Distance(transform.position, targetPos) <= reachDistance)
        {
            if (animator != null)
                animator.SetBool("Bitting", true);

            _targetNutrient.SetActive(false);
            _targetNutrient = null;
            _eatTimer = eatDuration;
            _state = State.Eating;
        }
    }

    // =========================================================
    // EATING
    // =========================================================
    // Enemy stays in place for eatDuration seconds (Bitting animation plays),
    // then heads back to the spline.
    void HandleEating()
    {
        _eatTimer -= Time.deltaTime;
        if (_eatTimer <= 0f)
        {
            if (animator != null)
                animator.SetBool("Bitting", false);

            _state = State.ReturningToPath;
        }
    }

    // =========================================================
    // RETURNING TO PATH
    // =========================================================
    void HandleReturningToPath()
    {
        if (splineContainer == null)
        {
            _state = State.Patrol;
            return;
        }

        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            splineContainer.transform.InverseTransformPoint(transform.position),
            out _, out float nearestT);

        Vector3 nearestWorld = GetSplinePositionWorld(nearestT);

        MoveToward(nearestWorld, returnSpeed * slowMultiplier, restrictY: false);

        if (Vector3.Distance(transform.position, nearestWorld) <= returnReachDistance)
        {
            _splineT = nearestT;
            _cooldownTimer = postReturnCooldown;
            _state = State.Patrol;
        }
    }

    // =========================================================
    // SPLINE HELPERS
    // =========================================================
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

    // =========================================================
    // MOVEMENT / ROTATION
    // =========================================================
    void MoveToward(Vector3 destination, float moveSpeed, bool restrictY = true)
    {
        Vector3 dir = destination - transform.position;
        if (restrictY) dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.position += dir.normalized * moveSpeed * Time.deltaTime;
        UpdateFacingAnimator(dir);
    }

    // Updates the Animator's LastX/LastY floats from the current movement
    // direction on the X/Z plane. Only updates while actually moving - while
    // idle the previous (non-zero) direction is kept, so idle animations still
    // face the direction the enemy was last walking. This never touches
    // transform.rotation; facing/rotation is entirely the billboard script's job.
    void UpdateFacingAnimator(Vector3 dir)
    {
        if (animator == null) return;

        Vector2 flatDir = new Vector2(dir.x, dir.z);
        if (flatDir.sqrMagnitude < 0.0001f) return;

        flatDir.Normalize();
        animator.SetFloat(LastXHash, flatDir.x);
        animator.SetFloat(LastYHash, flatDir.y);
    }

    public void FinihEat()
    {
        animator.SetBool("Bitting", false);
    }

    // =========================================================
    // GIZMOS
    // =========================================================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (splineContainer != null && Application.isPlaying)
        {
            Vector3 splinePos = GetSplinePositionWorld(_splineT);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(splinePos, 0.2f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, splinePos);
        }

#if UNITY_EDITOR
        string label = $"State: {_state}\nHP: {currentHealth}/{maxHealth}"
                     + $"\nSlowed: {(IsSlowed ? $"{slowMultiplier:F2}x for {slowTimer:F2}s" : "no")}";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, label);
#endif
    }
    public void Freeze(float duration)
    {
        if (_isDead) return;

        freezeTimer = Mathf.Max(freezeTimer, duration);
    }
}