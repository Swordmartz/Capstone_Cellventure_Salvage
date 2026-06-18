using UnityEngine;

public class HealWinConditionManager : MonoBehaviour
{
    [Header("Healables to Track")]
    public HealTarget[] healTargets;

    [Header("Reward")]
    public GameObject starRating;

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
        foreach (HealTarget target in healTargets)
        {
            if (target == null) continue;

            if (!target.IsFullyHealed)
                return; // at least one isn't fully healed yet
        }

        if (starRating != null)
            starRating.SetActive(true);
    }
}