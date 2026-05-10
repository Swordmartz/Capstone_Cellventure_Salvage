using UnityEngine;
using System.IO;

public class PlayerLogger : MonoBehaviour
{
    [Header("References")]
    public AI_TestTD halu;
    public GameTimer gameTimer;

    [Header("Settings")]
    public string fileName = "PlayerLog";

    [Header("Testing")]
    public bool forceLog = false;

    private string filePath;
    private bool hasLogged = false;

    private void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, fileName + ".csv");

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "PerformanceScore,IdleTime,FailedDelivery,CompletionTime\n");
            Debug.Log("CSV created at: " + filePath);
        }
    }

    private void Update()
    {
        // ✅ Force log for testing
        if (forceLog)
        {
            forceLog = false;
            hasLogged = false;
            LogSession();
            return;
        }

        if (gameTimer == null) return;
        if (hasLogged) return;

        if (!gameTimer.timerActive && gameTimer.GetCurrentTime() <= 0f)
        {
            LogSession();
            hasLogged = true;
        }
    }

    public void LogSession()
    {
        if (halu == null)
        {
            Debug.LogWarning("PlayerLogger: halu reference is missing!");
            return;
        }

        string row = $"{halu.performanceScore},{halu.idleTime},{halu.FailedDelivery},{halu.comptTime}\n"; // ✅ Using halu.comptTime directly

        File.AppendAllText(filePath, row);
        Debug.Log("Session logged: " + row);
        Debug.Log("CSV location: " + filePath);
    }

    public void ResetLogger()
    {
        hasLogged = false;
    }
}