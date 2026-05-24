using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager _;
    [SerializeField] private bool _debugMode;
    public enum MainMenuButton { play, settings, almanac, quit, awards }
    public enum SettingButtons { back }
    public enum AlmanacButtons { back }

    [SerializeField] private RFadeManager fadeManage;
    [SerializeField] private GameObject _MainMenuContainer;
    [SerializeField] private GameObject _SettingsContainer;
    [SerializeField] private GameObject _AlmanacContainer;
    [SerializeField] private GameObject _AwardsContainer;

    [SerializeField] private string _sceneToLoad;
    public void Awake()
    {
        if (_ == null)
        {
            _ = this;
        }
        else
        {
            Debug.LogError("There are more than one MainMenuManager's in the scene");
        }
    }
    private void Start()
    {
        OpenMenu(_MainMenuContainer);
    }
    public void MainMenuButtonClicked(MainMenuButton buttonClicked)
    {
        DebugMessage ("Button Clicked: " + buttonClicked.ToString ());
        switch (buttonClicked)
        {
            case MainMenuButton.play:
                PlayClicked();
                break;
            case MainMenuButton.settings:
                SettingsClicked();
                break;
            case MainMenuButton.almanac:
                AlmanacClicked();
                break;
            case MainMenuButton.quit: 
                QuitGame();
                break;
            case MainMenuButton.awards:
                AwardsClicked();
                break;
            default:
                Debug.Log("The button clicked wasn't implemnted in MainMenuManager Method");
                break;
        }


    }
    public void ReturnToMainMenu() 
    {
        OpenMenu(_MainMenuContainer);
    }
    public void PlayClicked()
    {

        StartCoroutine(LoadSceneFade());
    }
    public void SettingsButtonClicked(SettingButtons buttonClicked)
    {
        switch (buttonClicked)
        {
            case SettingButtons.back:
                ReturnToMainMenu();
                break;
        }
    }
    public void SettingsClicked()
    {
        OpenMenu(_SettingsContainer);
        

    }
    public void AlmanacButtonClicked(AlmanacButtons buttonClicked)
    {
        switch (buttonClicked)
        {
            case AlmanacButtons.back:
                ReturnToMainMenu();
                break;
        }
    }
    public void AlmanacClicked()
    {
        OpenMenu(_AlmanacContainer);
    }

    public void AwardsClicked()
    {
        OpenMenu(_AwardsContainer);
    }

    private void DebugMessage(string message)
    {
        if (_debugMode)
        {
            Debug.Log(message);
        }
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
    public void OpenMenu(GameObject menuToOpen)
    {
        _MainMenuContainer.SetActive(menuToOpen == _MainMenuContainer);
        _AlmanacContainer.SetActive(menuToOpen == _AlmanacContainer);
        _SettingsContainer.SetActive(menuToOpen == _SettingsContainer);
        _AwardsContainer.SetActive(menuToOpen == _AwardsContainer);
    }

   IEnumerator LoadSceneFade()
   {
        fadeManage.DoFade(0f, 1.5f, 1f, 0f);
        yield return  new WaitForSeconds(1f);
        SceneManager.LoadScene(_sceneToLoad);

    }

}
