using UnityEngine;
using UnityEngine.UI;

public class LevelButtonLock : MonoBehaviour
{
    [Header("Level")]
    [Tooltip("Must match the ID used by LevelUnlockManager.")]
    [SerializeField]
    private string levelID = "Level2";

    [Header("Button")]
    [SerializeField]
    private Button levelButton;


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        // Automatically find the Button if one wasn't assigned.
        if (levelButton == null)
        {
            levelButton = GetComponent<Button>();
        }
    }


    // ============================================================
    // ENABLE
    // ============================================================

    private void OnEnable()
    {
        RefreshButton();
    }


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        RefreshButton();
    }


    // ============================================================
    // REFRESH BUTTON
    // ============================================================

    public void RefreshButton()
    {
        if (levelButton == null)
        {
            Debug.LogError(
                "[LevelButtonLock] " +
                "No Button component found on " +
                gameObject.name
            );

            return;
        }


        // --------------------------------------------------------
        // FIND LEVEL MANAGER
        // --------------------------------------------------------

        LevelUnlockManager manager =
            LevelUnlockManager.Instance;


        if (manager == null)
        {
            Debug.LogError(
                "[LevelButtonLock] " +
                "LevelUnlockManager does not exist!"
            );

            levelButton.interactable = false;

            return;
        }


        // --------------------------------------------------------
        // CHECK LEVEL
        // --------------------------------------------------------

        bool unlocked =
            manager.IsLevelUnlocked(levelID);


        // --------------------------------------------------------
        // ENABLE / DISABLE BUTTON
        // --------------------------------------------------------

        levelButton.interactable = unlocked;


        Debug.Log(
            "[LevelButtonLock] " +
            levelID +
            " = " +
            (unlocked
                ? "INTERACTABLE"
                : "LOCKED")
        );
    }
}