using System.Collections.Generic;
using UnityEngine;

public class HealTargetB : MonoBehaviour
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

    [Header("Passerby Heal-On-Interact")]
    [Tooltip("Radius around this target to look for passersby to call over when interacted with.")]
    public float passerbyDetectionRange = 4f;
    [Tooltip("How close a diverted passerby must get before they're considered 'arrived' and heal is applied.")]
    public float passerbyArriveDistance = 0.25f;
    [Tooltip("HP restored for each passerby that reaches this target.")]
    public float healAmountPerPasserby = 15f;

    [Header("Super Heal Stack")]
    [Tooltip("Drag in the SuperHealAbility object. Its stack goes up each time this target is healed.")]
    public SuperHealAbility superHealAbility;
    [Tooltip("How much stack to add per heal event (per passerby that arrives, or per Tap heal).")]
    public int stackPerHeal = 1;

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

    /// <summary>
    /// Call this on interact (button press, tap, etc). Finds any passersby
    /// (of the B variant only) within passerbyDetectionRange and sends them
    /// straight here to heal, bypassing their normal spline path.
    /// </summary>
    public void Interact()
    {
        if (isDead || isFullyHealed) return;

        List<PasserbySplinePathB> nearby = FindNearbyPasserbys();

        foreach (var passerby in nearby)
        {
            passerby.DivertToHeal(transform, passerbyArriveDistance, OnPasserbyReachedHealTarget);
        }
    }

    // Retained for existing UI hookups — now routes into Interact().
    public void Tap()
    {
        Interact();
    }

    private List<PasserbySplinePathB> FindNearbyPasserbys()
    {
        var result = new List<PasserbySplinePathB>();

        foreach (var passerby in PasserbySplinePathB.ActiveInstances)
        {
            if (passerby == null || !passerby.IsAvailableForHeal) continue;

            float distance = Vector3.Distance(passerby.transform.position, transform.position);
            if (distance <= passerbyDetectionRange)
                result.Add(passerby);
        }

        return result;
    }

    private void OnPasserbyReachedHealTarget(PasserbySplinePathB passerby)
    {
        if (!isDead && !isFullyHealed)
        {
            currentHP += healAmountPerPasserby;

            if (currentHP >= maxHP)
            {
                currentHP = maxHP;
                isFullyHealed = true;
                NotifyWinCondition();
            }

            // This target just got healed — feed the super stack.
            AddSuperStack();
        }

        // Passerby is consumed once it delivers the heal.
        if (passerby != null)
            Destroy(passerby.gameObject);
    }

    public void FullyHeal()
    {
        if (isDead) return; // remove this line if you want the super to revive dead targets

        currentHP = maxHP;
        isFullyHealed = true;
        NotifyWinCondition();
    }

    /// <summary>
    /// Adds stack to the linked SuperHealAbility, if one is assigned.
    /// </summary>
    private void AddSuperStack()
    {
        if (superHealAbility != null)
            superHealAbility.AddStack(stackPerHeal);
    }

    /// <summary>
    /// Tells the HealWinConditionManager (if present in the scene) to
    /// re-check whether all tracked targets are now fully healed.
    /// </summary>
    private void NotifyWinCondition()
    {
        if (HealWinConditionManager.Instance != null)
            HealWinConditionManager.Instance.CheckAllHealed();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, healRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, passerbyDetectionRange);
    }
}