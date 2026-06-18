using UnityEngine;
using System.Collections.Generic;

public class WinConditionManager : MonoBehaviour
{
    [Header("Enemies to Track")]
    public GameObject[] enemies;

    [Header("Reward")]
    public GameObject starRating;

    public static WinConditionManager Instance { get; private set; }

    private HashSet<GameObject> defeatedEnemies = new HashSet<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (starRating != null)
            starRating.SetActive(false);
    }

    // Call this when a specific enemy is defeated/deactivated
    public void ReportEnemyDefeated(GameObject enemy)
    {
        if (enemy == null) return;

        defeatedEnemies.Add(enemy);
        CheckAllEnemiesDefeated();
    }

    private void CheckAllEnemiesDefeated()
    {
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            if (!defeatedEnemies.Contains(enemy))
                return; // at least one hasn't been defeated yet
        }

        if (starRating != null)
            starRating.SetActive(true);
    }
}