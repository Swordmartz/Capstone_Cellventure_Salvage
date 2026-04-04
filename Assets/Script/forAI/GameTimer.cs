using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Import TextMeshPro namespace

public class GameTimer : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text timerText;             // assign a TMP_Text for countdown
    public GameObject victoryScreenUI;     // assign victory panel
    public GameObject failScreenUI;        // assign fail panel

    [Header("Timer Settings")]
    public float missionTime = 60f;        // total mission time in seconds

    private float currentTime;
    private bool missionEnded = false;

    void Start()
    {
        currentTime = missionTime;
    }

    void Update()
    {
        if (missionEnded) return;

        currentTime -= Time.deltaTime;
        timerText.text = Mathf.Ceil(currentTime).ToString();

        if (currentTime <= 0f)
        {
            FailMission();
        }
    }

    // Call this when mission objectives are completed
    public void FulfillMission()
    {
        missionEnded = true;
        Time.timeScale = 0f; // freeze gameplay
        victoryScreenUI.SetActive(true);
    }

    void FailMission()
    {
        missionEnded = true;
        Time.timeScale = 0f; // freeze gameplay
        failScreenUI.SetActive(true);
    }

    // Optional: buttons on victory/fail screens
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // replace with your main menu scene name
    }
}
