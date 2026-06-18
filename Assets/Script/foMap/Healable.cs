using UnityEngine;

public class HealTarget : MonoBehaviour
{
    [Header("Settings")]
    public float maxHP = 100f;
    public float drainPerSecond = 5f;
    public float healPerTap = 10f;

    [Header("Range")]
    public Transform player;
    public float healRange = 3f;

    [Header("Runtime Info (read only)")]
    [SerializeField] private float currentHP;
    [SerializeField] private bool isDead;
    [SerializeField] private bool isFullyHealed;

    [Header("Mission Submission")]
    public MissionSubmissionManager missionManager;
    public int missionIndex = 0;

    public float CurrentHP => currentHP;
    public bool IsDead => isDead;
    public bool IsFullyHealed => isFullyHealed;
    public bool PlayerInRange => player != null &&
                                 Vector3.Distance(transform.position, player.position) <= healRange;

    private float drainTimer;

    private void Awake()
    {
        currentHP = maxHP;
        isDead = false;
        isFullyHealed = false;
        drainTimer = 0f;
    }

    private void Update()
    {
        if (isDead || isFullyHealed) return;

        drainTimer += Time.deltaTime;

        if (drainTimer >= 1f)
        {
            drainTimer -= 1f;
            currentHP -= drainPerSecond;
            currentHP = Mathf.Max(currentHP, 0f);

            if (currentHP <= 0f)
                isDead = true;
        }
    }

    public void Tap()
    {
        if (isDead || isFullyHealed) return;
        if (!PlayerInRange) return;

        currentHP = Mathf.Min(Mathf.Round(currentHP + healPerTap), maxHP);

        if (currentHP >= maxHP)
        {
            isFullyHealed = true;

            if (missionManager != null)
            {
                missionManager.CompleteMissionByIndex(missionIndex);
                Debug.Log("[HealTarget] Fully healed — Mission " + missionIndex + " completed.");
            }

            if (HealWinConditionManager.Instance != null)
                HealWinConditionManager.Instance.CheckAllHealed();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, healRange);
    }
}