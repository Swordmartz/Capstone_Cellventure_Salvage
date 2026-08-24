using UnityEngine;

public class AchievementUnlockPanel : MonoBehaviour
{
    [Header("Achievement")]

    [Tooltip("The unique ID of the achievement this panel unlocks.")]
    [SerializeField]
    private string achievementID = "A1";


    [Header("Settings")]

    [Tooltip("If true, this panel will only attempt to unlock the achievement once.")]
    [SerializeField]
    private bool unlockOnlyOnce = true;


    private bool hasProcessed = false;


    // ============================================================
    // ON ENABLE
    // ============================================================

    private void OnEnable()
    {
        Debug.Log(
            "[AchievementUnlockPanel] Panel activated: " +
            gameObject.name
        );

        UnlockAchievement();
    }


    // ============================================================
    // UNLOCK ACHIEVEMENT
    // ============================================================

    private void UnlockAchievement()
    {
        // --------------------------------------------------------
        // PREVENT REPEATED PROCESSING
        // --------------------------------------------------------

        if (unlockOnlyOnce && hasProcessed)
        {
            Debug.Log(
                "[AchievementUnlockPanel] " +
                achievementID +
                " has already been processed."
            );

            return;
        }


        // --------------------------------------------------------
        // CHECK ID
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(achievementID))
        {
            Debug.LogError(
                "[AchievementUnlockPanel] " +
                "Achievement ID is empty!"
            );

            return;
        }


        // --------------------------------------------------------
        // GET MANAGER
        // --------------------------------------------------------

        AchievementManager manager =
            AchievementManager.Instance;


        if (manager == null)
        {
            Debug.LogError(
                "[AchievementUnlockPanel] " +
                "AchievementManager does not exist!"
            );

            return;
        }


        // --------------------------------------------------------
        // CHECK IF ALREADY UNLOCKED
        // --------------------------------------------------------

        if (manager.IsUnlocked(achievementID))
        {
            Debug.Log(
                "[AchievementUnlockPanel] " +
                achievementID +
                " is already unlocked."
            );

            hasProcessed = true;

            return;
        }


        // --------------------------------------------------------
        // UNLOCK
        // --------------------------------------------------------

        Debug.Log(
            "[AchievementUnlockPanel] " +
            "Unlocking achievement: " +
            achievementID
        );


        manager.UnlockAchievement(
            achievementID
        );


        // --------------------------------------------------------
        // MARK AS PROCESSED
        // --------------------------------------------------------

        hasProcessed = true;


        Debug.Log(
            "[AchievementUnlockPanel] " +
            "Achievement confirmed: " +
            achievementID
        );
    }
}