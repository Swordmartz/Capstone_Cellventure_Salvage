using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private RFadeManager fadeManager; // assign your fade manager in Inspector
    public GameObject pauseMenuUI; // assign your pause menu panel in Inspector
    private bool isPaused = false;

    // Called by your UI Pause Button
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    void PauseGame()
    {
        pauseMenuUI.SetActive(true);   // show menu
        Time.timeScale = 0f;           // freeze gameplay
        isPaused = true;
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);  // hide menu
        Time.timeScale = 1f;           // resume gameplay
        isPaused = false;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f; // reset time before reload
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // reset time before switching
        StartCoroutine(LoadSceneWithFade());
    }
    IEnumerator LoadSceneWithFade()
    {
        fadeManager.DoFade(0, 1f, .2f, 0f);
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("MainMenu"); // replace with your main menu scene name
    }
}
