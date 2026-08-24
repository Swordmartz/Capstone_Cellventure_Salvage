using UnityEngine;

public class AchievementDisplay : MonoBehaviour
{
    [Header("Achievement")]

    [Tooltip("Unique ID of this achievement.")]
    public string achievementID = "A1";


    [Header("Objects")]

    [Tooltip("Object that should be ACTIVE after unlocking.")]
    public GameObject unlockedObject;


    [Tooltip("Object that should be ACTIVE before unlocking.")]
    public GameObject lockedObject;


    // ============================================================
    // ON ENABLE
    // ============================================================

    private void OnEnable()
    {
        Debug.Log(
            "[AchievementDisplay] Enabled: " +
            achievementID
        );

        RegisterAndRefresh();
    }


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        RegisterAndRefresh();
    }


    // ============================================================
    // ON DISABLE
    // ============================================================

    private void OnDisable()
    {
        AchievementManager manager =
            AchievementManager.Instance;

        if (manager != null)
        {
            manager.UnregisterDisplay(this);
        }
    }


    // ============================================================
    // REGISTER AND REFRESH
    // ============================================================

    private void RegisterAndRefresh()
    {
        AchievementManager manager =
            AchievementManager.Instance;


        // --------------------------------------------------------
        // MANAGER DOES NOT EXIST
        // --------------------------------------------------------

        if (manager == null)
        {
            Debug.LogWarning(
                "[AchievementDisplay] " +
                "AchievementManager does not exist yet. " +
                "ID: " +
                achievementID
            );

            return;
        }


        // --------------------------------------------------------
        // REGISTER WITH MANAGER
        // --------------------------------------------------------

        manager.RegisterDisplay(this);


        // --------------------------------------------------------
        // GET CURRENT ACHIEVEMENT STATE
        // --------------------------------------------------------

        bool unlocked =
            manager.IsUnlocked(achievementID);


        // --------------------------------------------------------
        // UPDATE VISUALS
        // --------------------------------------------------------

        UpdateDisplay(unlocked);
    }


    // ============================================================
    // UPDATE DISPLAY
    // ============================================================

    public void UpdateDisplay(bool unlocked)
    {
        Debug.Log(
            "[AchievementDisplay] Updating " +
            achievementID +
            " -> " +
            (unlocked
                ? "UNLOCKED"
                : "LOCKED")
        );


        // --------------------------------------------------------
        // UNLOCKED OBJECT
        // --------------------------------------------------------

        if (unlockedObject != null)
        {
            unlockedObject.SetActive(
                unlocked
            );
        }
        else
        {
            Debug.LogError(
                "[AchievementDisplay] " +
                achievementID +
                " has no UNLOCKED object assigned!"
            );
        }


        // --------------------------------------------------------
        // LOCKED OBJECT
        // --------------------------------------------------------

        if (lockedObject != null)
        {
            lockedObject.SetActive(
                !unlocked
            );
        }
        else
        {
            Debug.LogError(
                "[AchievementDisplay] " +
                achievementID +
                " has no LOCKED object assigned!"
            );
        }
    }


    // ============================================================
    // GET ACHIEVEMENT ID
    // ============================================================

    public string GetAchievementID()
    {
        return achievementID;
    }
}