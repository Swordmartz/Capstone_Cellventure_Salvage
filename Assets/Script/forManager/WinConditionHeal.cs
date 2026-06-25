using UnityEngine;

public class HealWinConditionManager : MonoBehaviour
{
    [Header("Healables to Track")]
    public HealTargetB[] healTargets;

    [Header("Reward")]
    public GameObject starRating;

    [Header("Debug")]
    [Tooltip("If true, logs to the Console which target is blocking the win condition.")]
    public bool debugLogging = false;

    public static HealWinConditionManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (starRating != null)
            starRating.SetActive(false);
    }

    // Call this whenever a HealTarget becomes fully healed
    public void CheckAllHealed()
    {
        foreach (HealTargetB target in healTargets)
        {
            if (target == null) continue;

            if (!target.IsFullyHealed)
            {
                if (debugLogging)
                    Debug.Log($"[HealWinConditionManager] Waiting on '{target.name}' — not fully healed yet.");

                return; // at least one isn't fully healed yet
            }
        }

        if (debugLogging)
            Debug.Log("[HealWinConditionManager] All targets fully healed — activating star rating.");

        if (starRating != null)
            starRating.SetActive(true);
    }
}