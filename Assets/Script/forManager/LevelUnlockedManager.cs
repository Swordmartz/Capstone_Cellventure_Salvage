using System.Collections.Generic;
using UnityEngine;

public class LevelUnlockManager : MonoBehaviour
{
    // ============================================================
    // SINGLETON
    // ============================================================

    public static LevelUnlockManager Instance { get; private set; }


    // ============================================================
    // SETTINGS
    // ============================================================

    private const string SavePrefix = "LEVEL_UNLOCKED_";


    // ============================================================
    // MEMORY
    // ============================================================

    private readonly HashSet<string> unlockedLevels =
        new HashSet<string>();


    // ============================================================
    // CREATE MANAGER AUTOMATICALLY
    // ============================================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad
    )]
    private static void CreateManager()
    {
        if (Instance != null)
            return;


        GameObject managerObject =
            new GameObject("LevelUnlockManager");


        managerObject.AddComponent<LevelUnlockManager>();
    }


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;


        DontDestroyOnLoad(gameObject);


        Debug.Log(
            "[LevelUnlockManager] " +
            "Persistent Level Unlock Manager ready."
        );
    }


    // ============================================================
    // UNLOCK LEVEL
    // ============================================================

    public void UnlockLevel(string levelID)
    {
        if (string.IsNullOrWhiteSpace(levelID))
        {
            Debug.LogError(
                "[LevelUnlockManager] " +
                "Cannot unlock level because the ID is empty."
            );

            return;
        }


        levelID = levelID.Trim();


        // --------------------------------------------------------
        // ALREADY UNLOCKED
        // --------------------------------------------------------

        if (IsLevelUnlocked(levelID))
        {
            Debug.Log(
                "[LevelUnlockManager] " +
                levelID +
                " is already unlocked."
            );

            return;
        }


        // --------------------------------------------------------
        // SAVE IN MEMORY
        // --------------------------------------------------------

        unlockedLevels.Add(levelID);


        // --------------------------------------------------------
        // SAVE PERMANENTLY
        // --------------------------------------------------------

        PlayerPrefs.SetInt(
            SavePrefix + levelID,
            1
        );

        PlayerPrefs.Save();


        Debug.Log(
            "[LevelUnlockManager] " +
            "LEVEL UNLOCKED: " +
            levelID
        );
    }


    // ============================================================
    // CHECK LEVEL
    // ============================================================

    public bool IsLevelUnlocked(string levelID)
    {
        if (string.IsNullOrWhiteSpace(levelID))
            return false;


        levelID = levelID.Trim();


        // Check memory first

        if (unlockedLevels.Contains(levelID))
            return true;


        // Check PlayerPrefs

        int savedValue =
            PlayerPrefs.GetInt(
                SavePrefix + levelID,
                0
            );


        if (savedValue == 1)
        {
            unlockedLevels.Add(levelID);

            return true;
        }


        return false;
    }


    // ============================================================
    // LOCK LEVEL
    // ============================================================

    public void LockLevel(string levelID)
    {
        if (string.IsNullOrWhiteSpace(levelID))
            return;


        levelID = levelID.Trim();


        unlockedLevels.Remove(levelID);


        PlayerPrefs.DeleteKey(
            SavePrefix + levelID
        );


        PlayerPrefs.Save();


        Debug.Log(
            "[LevelUnlockManager] " +
            "LEVEL LOCKED: " +
            levelID
        );
    }


    // ============================================================
    // RESET ALL LEVELS
    // ============================================================

    public void ResetAllLevels()
    {
        List<string> keysToDelete =
            new List<string>();


        foreach (string levelID in unlockedLevels)
        {
            keysToDelete.Add(
                SavePrefix + levelID
            );
        }


        for (int i = 0; i < keysToDelete.Count; i++)
        {
            PlayerPrefs.DeleteKey(
                keysToDelete[i]
            );
        }


        unlockedLevels.Clear();


        PlayerPrefs.Save();


        Debug.Log(
            "[LevelUnlockManager] " +
            "ALL LEVELS RESET."
        );
    }
}