using UnityEngine;

public class UnlockLevelOnAwake : MonoBehaviour
{
    [Header("Level To Unlock")]

    [Tooltip(
        "The ID of the level that should become unlocked."
    )]
    [SerializeField]
    private string levelID = "Level2";


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        if (LevelUnlockManager.Instance == null)
        {
            Debug.LogError(
                "[UnlockLevelOnAwake] " +
                "LevelUnlockManager does not exist!"
            );

            return;
        }


        LevelUnlockManager.Instance.UnlockLevel(
            levelID
        );


        Debug.Log(
            "[UnlockLevelOnAwake] " +
            "Unlocked level: " +
            levelID
        );
    }
}