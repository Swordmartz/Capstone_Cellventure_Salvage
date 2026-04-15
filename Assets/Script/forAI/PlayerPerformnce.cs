using UnityEngine;

/// <summary>
/// Holds the player's performance data for the current level.
/// Set these values from your game logic before calling StarRatingManager.EvaluateScore()
/// </summary>
public class PlayerPerformanceData
{
    public float completionTime;       // Time in seconds the player took
    public float maxTime;              // Max allowed time for full evaluation
    public float performanceScore;     // 0-100, e.g. enemies killed, damage taken etc.
}