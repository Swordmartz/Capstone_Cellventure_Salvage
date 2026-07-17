using UnityEngine;
using System.Collections;

public class WinConditionManager : MonoBehaviour
{
    [Header("Enemies to Track")]
    public GameObject[] enemies;

    [Header("Reward")]
    public GameObject starRating;

    [Header("Dialogue")]
    public AIforDialogue aiScript;

    public static WinConditionManager Instance { get; private set; }

    // Prevents the dialogue/star sequence from firing more than once,
    // e.g. if an enemy is reactivated and defeated again later.
    private bool winSequenceTriggered = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (starRating != null)
            starRating.SetActive(false);
    }

    // Call this when a specific enemy is defeated/deactivated.
    // The GameObject itself isn't tracked in a set anymore -- this just
    // acts as a trigger to re-check everyone's IsDead flag.
    public void ReportEnemyDefeated(GameObject enemy)
    {
        if (enemy == null) return;

        Debug.Log($"[WinCondition] Enemy defeated reported: {enemy.name}");
        CheckAllEnemiesDefeated();
    }

    private void CheckAllEnemiesDefeated()
    {
        if (winSequenceTriggered)
            return; // already handled, don't re-run dialogue/star rating

        if (enemies == null || enemies.Length == 0)
        {
            Debug.LogWarning("[WinCondition] 'enemies' array is empty -- nothing to check.");
            return;
        }

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            DetectionFSM fsm = enemy.GetComponent<DetectionFSM>();
            if (fsm == null)
            {
                Debug.LogWarning($"[WinCondition] {enemy.name} has no DetectionFSM component -- skipping.");
                continue;
            }

            if (!fsm.isDead)
            {
                Debug.Log($"[WinCondition] {enemy.name} not yet defeated -- win condition blocked.");
                return; // at least one hasn't been defeated yet
            }
        }

        Debug.Log("[WinCondition] All enemies defeated -- triggering win sequence.");
        winSequenceTriggered = true;
        StartCoroutine(ShowStarRatingAfterDialogue());
    }

    private IEnumerator ShowStarRatingAfterDialogue()
    {
        if (aiScript != null)
            yield return aiScript.StartCoroutine(aiScript.Dialogue7IWBCE());
        else
            Debug.LogWarning("AI script not assigned!");

        if (starRating != null)
            starRating.SetActive(true);
    }
}