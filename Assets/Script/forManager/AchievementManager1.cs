using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    private readonly HashSet<string> unlockedAchievements =
        new HashSet<string>();

    private readonly List<AchievementDisplay> displays =
        new List<AchievementDisplay>();

    private const string SavePrefix = "ACHIEVEMENT_";


    // ============================================================
    // AUTOMATICALLY CREATE PERSISTENT MANAGER
    // ============================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateManager()
    {
        if (Instance != null)
            return;

        GameObject managerObject =
            new GameObject("AchievementManager");

        managerObject.AddComponent<AchievementManager>();
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

        LoadAllSavedAchievements();

        Debug.Log(
            "[AchievementManager] Persistent Achievement Manager ready."
        );
    }


    // ============================================================
    // LOAD SAVED ACHIEVEMENTS
    // ============================================================

    private void LoadAllSavedAchievements()
    {
        // We don't need to know every achievement beforehand.
        // Individual achievements are loaded when IsUnlocked()
        // is called.
    }


    // ============================================================
    // UNLOCK ACHIEVEMENT
    // ============================================================

    public void UnlockAchievement(string achievementID)
    {
        if (string.IsNullOrWhiteSpace(achievementID))
        {
            Debug.LogError(
                "[AchievementManager] " +
                "Cannot unlock achievement because the ID is empty."
            );

            return;
        }

        achievementID = achievementID.Trim();


        // --------------------------------------------------------
        // ALREADY UNLOCKED
        // --------------------------------------------------------

        if (IsUnlocked(achievementID))
        {
            Debug.Log(
                "[AchievementManager] " +
                achievementID +
                " is already unlocked."
            );

            return;
        }


        // --------------------------------------------------------
        // UNLOCK
        // --------------------------------------------------------

        unlockedAchievements.Add(achievementID);


        // --------------------------------------------------------
        // SAVE PERMANENTLY
        // --------------------------------------------------------

        PlayerPrefs.SetInt(
            SavePrefix + achievementID,
            1
        );

        PlayerPrefs.Save();


        Debug.Log(
            "[AchievementManager] ACHIEVEMENT UNLOCKED: " +
            achievementID
        );


        // --------------------------------------------------------
        // UPDATE CURRENT SCENE
        // --------------------------------------------------------

        UpdateDisplays(achievementID);
    }


    // ============================================================
    // CHECK IF ACHIEVEMENT IS UNLOCKED
    // ============================================================

    public bool IsUnlocked(string achievementID)
    {
        if (string.IsNullOrWhiteSpace(achievementID))
            return false;

        achievementID = achievementID.Trim();


        // Check memory first

        if (unlockedAchievements.Contains(achievementID))
            return true;


        // Check saved value

        int savedValue =
            PlayerPrefs.GetInt(
                SavePrefix + achievementID,
                0
            );


        if (savedValue == 1)
        {
            unlockedAchievements.Add(
                achievementID
            );

            return true;
        }


        return false;
    }


    // ============================================================
    // REGISTER DISPLAY
    // ============================================================

    public void RegisterDisplay(
    AchievementDisplay display
)
    {
        if (display == null)
            return;


        CleanupDisplays();


        if (!displays.Contains(display))
        {
            displays.Add(display);

            Debug.Log(
                "[AchievementManager] Registered display: " +
                display.GetAchievementID()
            );
        }


        string achievementID =
            display.GetAchievementID();


        bool unlocked =
            IsUnlocked(achievementID);


        Debug.Log(
            "[AchievementManager] Display " +
            achievementID +
            " state = " +
            (unlocked ? "UNLOCKED" : "LOCKED")
        );


        display.UpdateDisplay(
            unlocked
        );
    }


    // ============================================================
    // UNREGISTER DISPLAY
    // ============================================================

    public void UnregisterDisplay(
        AchievementDisplay display
    )
    {
        if (display == null)
            return;

        displays.Remove(display);
    }


    // ============================================================
    // UPDATE DISPLAYS
    // ============================================================

    private void UpdateDisplays(
        string achievementID
    )
    {
        CleanupDisplays();


        bool unlocked =
            IsUnlocked(achievementID);


        for (int i = 0; i < displays.Count; i++)
        {
            AchievementDisplay display =
                displays[i];

            if (display == null)
                continue;


            if (
                display.achievementID ==
                achievementID
            )
            {
                display.UpdateDisplay(
                    unlocked
                );
            }
        }
    }


    // ============================================================
    // CLEANUP DESTROYED SCENE REFERENCES
    // ============================================================

    private void CleanupDisplays()
    {
        for (int i = displays.Count - 1; i >= 0; i--)
        {
            if (displays[i] == null)
            {
                displays.RemoveAt(i);
            }
        }
    }


    // ============================================================
    // FORCE REFRESH ALL DISPLAYS
    // ============================================================

    public void RefreshAllDisplays()
    {
        CleanupDisplays();


        for (int i = 0; i < displays.Count; i++)
        {
            AchievementDisplay display =
                displays[i];

            if (display == null)
                continue;


            bool unlocked =
                IsUnlocked(
                    display.achievementID
                );


            display.UpdateDisplay(
                unlocked
            );
        }
    }


    // ============================================================
    // RESET ONE ACHIEVEMENT
    // ============================================================

    public void ResetAchievement(
        string achievementID
    )
    {
        if (string.IsNullOrWhiteSpace(achievementID))
            return;


        achievementID = achievementID.Trim();


        unlockedAchievements.Remove(
            achievementID
        );


        PlayerPrefs.DeleteKey(
            SavePrefix + achievementID
        );

        PlayerPrefs.Save();


        UpdateDisplays(
            achievementID
        );


        Debug.Log(
            "[AchievementManager] Reset achievement: " +
            achievementID
        );
    }


    // ============================================================
    // RESET ALL ACHIEVEMENTS
    // ============================================================

    public void ResetAllAchievements()
    {
        CleanupDisplays();


        // Only delete achievement keys.
        // DO NOT use PlayerPrefs.DeleteAll()
        // because that could delete your other game data.

        List<string> keysToDelete =
            new List<string>();


        foreach (
            string achievementID
            in unlockedAchievements
        )
        {
            keysToDelete.Add(
                SavePrefix + achievementID
            );
        }


        for (int i = 0; i < keysToDelete.Count; i++)
        {
            PlayerPrefs.DeleteKey(
                keysToDelete[i]
            );
        }


        unlockedAchievements.Clear();

        PlayerPrefs.Save();


        RefreshAllDisplays();


        Debug.Log(
            "[AchievementManager] " +
            "All achievements reset."
        );
    }
}