using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;

public class DetectionFSM : MonoBehaviour
{
    public enum EnemyState { Idle, Detecting, Hiding, Returning, Dead }
    public EnemyState currentState = EnemyState.Idle;

    private NavMeshAgent agent;
    public Transform player;
    public Transform originalTarget;
    public LayerMask hideableLayer;

    [Header("Detection Settings")]
    public float detectionRadius = 5f;
    public float searchRadius = 50f;

    [Header("Hide Settings")]
    public float hideTime = 3f;
    public float hideCooldown = 5f;

    [Header("Speed Settings")]
    public float normalSpeed = 3.5f;
    public float maxBoostSpeed = 8f;
    public int maxBoosts = 3;
    public float speedDecayRate = 1f;

    private Transform currentTarget;
    private bool canHide = true;
    private int hideCount = 0;

    [Header("Health Settings")]
    public int maxHealth = 10;
    public int currentHealth;
    public bool isInvincible = false;

    [Header("Attacked")]
    public bool isMarked { get; private set; } = false;

    [Header("Hit Flash")]
    public SpriteRenderer spriteRenderer;
    public float flashDuration = 0.3f;
    public int flashCount = 1;

    [Header("HP Bar")]
    public Slider hpBarSlider;

    private Coroutine _flashCoroutine;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = normalSpeed;

        if (originalTarget != null)
            agent.SetDestination(originalTarget.position);

        currentHealth = maxHealth;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (hpBarSlider != null)
        {
            hpBarSlider.maxValue = maxHealth;
            hpBarSlider.value = currentHealth;
        }
    }

    void Update()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            GameObject activePlayer = GameObject.FindGameObjectWithTag("Player");
            if (activePlayer != null)
                player = activePlayer.transform;
        }

        switch (currentState)
        {
            case EnemyState.Idle: HandleIdle(); break;
            case EnemyState.Detecting: HandleDetecting(); break;
            case EnemyState.Hiding: HandleHiding(); break;
            case EnemyState.Returning: HandleReturning(); break;
            case EnemyState.Dead: break;
        }
    }

    private void HandleIdle()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (canHide && distanceToPlayer <= detectionRadius)
            currentState = EnemyState.Detecting;
    }

    private void HandleDetecting()
    {
        if (currentTarget == null)
        {
            currentTarget = FindRandomHideable();
            if (currentTarget != null)
            {
                agent.SetDestination(currentTarget.position);

                float boostFactor = Mathf.Clamp01(1f - (float)hideCount / maxBoosts);
                float boostedSpeed = Mathf.Lerp(normalSpeed, maxBoostSpeed, boostFactor);
                StartCoroutine(ApplyBoostAfterStop(boostedSpeed));
            }
        }

        if (currentTarget != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            currentState = EnemyState.Hiding;
    }

    private void HandleHiding()
    {
        if (!isInvincible)
            StartCoroutine(HideTimer());
    }

    private void HandleReturning()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentTarget = null;
            currentState = EnemyState.Idle;
        }
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        if (hpBarSlider != null)
            hpBarSlider.value = currentHealth;

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRed());

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Don't apply boost if stunned
        if (!agent.isStopped)
        {
            float boostFactor = Mathf.Clamp01(1f - (float)hideCount / maxBoosts);
            float boostedSpeed = Mathf.Lerp(normalSpeed, maxBoostSpeed, boostFactor);
            StartCoroutine(ApplyBoostAfterStop(boostedSpeed));
        }
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
        _flashCoroutine = null;
    }

    public void Die()
    {
        currentState = EnemyState.Dead;
        currentHealth = 0;

        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        if (hpBarSlider != null)
        {
            hpBarSlider.value = 0;
            hpBarSlider.gameObject.SetActive(false);
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }
    }

    public void ForceKill()
    {
        currentHealth = 0;
        Die();
    }

    private IEnumerator HideTimer()
    {
        isInvincible = true;
        canHide = false;
        hideCount++;

        yield return new WaitForSeconds(hideTime);

        if (currentTarget != null)
        {
            Collider col = currentTarget.GetComponent<Collider>();
            if (col != null)
            {
                Bounds bounds = col.bounds;
                Vector3 randomPoint = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    bounds.center.y,
                    Random.Range(bounds.min.z, bounds.max.z)
                );

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
            }
        }

        if (originalTarget != null)
        {
            agent.SetDestination(originalTarget.position);
            currentState = EnemyState.Returning;
        }

        agent.speed = normalSpeed;
        isInvincible = false;

        yield return new WaitForSeconds(hideCooldown);
        canHide = true;
    }

    private Transform FindRandomHideable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, hideableLayer);
        if (hits.Length == 0) return null;

        int randomIndex = Random.Range(0, hits.Length);
        return hits[randomIndex].transform;
    }

    private IEnumerator ApplyBoostAfterStop(float boostedSpeed)
    {
        if (currentState == EnemyState.Dead) yield break;
        if (agent.isStopped) yield break;

        agent.speed = boostedSpeed;
        StartCoroutine(GraduallyReduceSpeed());
        yield break;
    }

    private IEnumerator GraduallyReduceSpeed()
    {
        while (agent.speed > normalSpeed && currentState != EnemyState.Hiding)
        {
            if (agent.isStopped) yield break;

            agent.speed -= speedDecayRate * Time.deltaTime;
            if (agent.speed < normalSpeed)
                agent.speed = normalSpeed;
            yield return null;
        }
    }

    public void ApplyStop(float duration)
    {
        if (agent == null) return;
        if (currentState == EnemyState.Dead) return;

        StartCoroutine(StopCoroutine(duration));
    }

    private IEnumerator StopCoroutine(float duration)
    {
        agent.isStopped = true;

        yield return new WaitForSeconds(duration);

        if (currentState == EnemyState.Dead) yield break;

        agent.isStopped = false;
        agent.speed = normalSpeed;
    }

    public void MarkAsHit()
    {
        isMarked = true;
    }

    public void ClearMark()
    {
        isMarked = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}