using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // ============================================================
    // SINGLETON
    // ============================================================

    public static MainMenuManager Instance { get; private set; }


    // ============================================================
    // DEBUG
    // ============================================================

    [SerializeField]
    private bool _debugMode = true;


    // ============================================================
    // ENUMS
    // ============================================================

    public enum MainMenuButton
    {
        play,
        settings,
        almanac,
        quit,
        awards
    }

    public enum SettingButtons
    {
        back
    }

    public enum AlmanacButtons
    {
        back
    }


    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("Managers")]

    [SerializeField]
    private RFadeManager fadeManage;


    [Header("Menu Containers")]

    [SerializeField]
    private GameObject _MainMenuContainer;

    [SerializeField]
    private GameObject _SettingsContainer;

    [SerializeField]
    private GameObject _AlmanacContainer;

    [SerializeField]
    private GameObject _AwardsContainer;


    [Header("Scene")]

    [SerializeField]
    private string _sceneToLoad;


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        Debug.Log(
            "========================================"
        );

        Debug.Log(
            "[MainMenuManager] Awake called on: " +
            gameObject.name
        );

        Debug.Log(
            "[MainMenuManager] Active in hierarchy: " +
            gameObject.activeInHierarchy
        );

        Debug.Log(
            "[MainMenuManager] Component enabled: " +
            enabled
        );


        // --------------------------------------------------------
        // CHECK FOR DUPLICATE
        // --------------------------------------------------------

        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "[MainMenuManager] DUPLICATE MainMenuManager FOUND!"
                + "\nExisting Manager: "
                + Instance.gameObject.name
                + "\nDuplicate Manager: "
                + gameObject.name
            );

            Destroy(gameObject);

            return;
        }


        // --------------------------------------------------------
        // ASSIGN INSTANCE
        // --------------------------------------------------------

        Instance = this;


        Debug.Log(
            "[MainMenuManager] Instance assigned successfully."
        );

        Debug.Log(
            "[MainMenuManager] Instance = " +
            Instance.gameObject.name
        );


        Debug.Log(
            "========================================"
        );
    }


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        DebugMessage(
            "MainMenuManager Start()"
        );


        // --------------------------------------------------------
        // CHECK REFERENCES
        // --------------------------------------------------------

        CheckReferences();


        // --------------------------------------------------------
        // OPEN MAIN MENU
        // --------------------------------------------------------

        OpenMenu(
            _MainMenuContainer
        );
    }


    // ============================================================
    // ON DESTROY
    // ============================================================

    private void OnDestroy()
    {
        Debug.Log(
            "[MainMenuManager] OnDestroy called on: " +
            gameObject.name
        );


        if (Instance == this)
        {
            Debug.Log(
                "[MainMenuManager] Clearing singleton Instance."
            );

            Instance = null;
        }
    }


    // ============================================================
    // MAIN MENU BUTTON CLICKED
    // ============================================================

    public void MainMenuButtonClicked(
        MainMenuButton buttonClicked
    )
    {
        DebugMessage(
            "Button Clicked: " +
            buttonClicked
        );


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

                Debug.LogWarning(
                    "[MainMenuManager] " +
                    "Button was not implemented."
                );

                break;
        }
    }


    // ============================================================
    // RETURN TO MAIN MENU
    // ============================================================

    public void ReturnToMainMenu()
    {
        DebugMessage(
            "Returning to Main Menu."
        );


        OpenMenu(
            _MainMenuContainer
        );
    }


    // ============================================================
    // PLAY
    // ============================================================

    public void PlayClicked()
    {
        DebugMessage(
            "Play button clicked."
        );


        // --------------------------------------------------------
        // CHECK THIS MANAGER
        // --------------------------------------------------------

        if (this == null)
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "PlayClicked was called on a destroyed manager."
            );

            return;
        }


        // --------------------------------------------------------
        // CHECK ENABLED STATE
        // --------------------------------------------------------

        if (!isActiveAndEnabled)
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "PlayClicked called while manager is inactive."
            );

            return;
        }


        // --------------------------------------------------------
        // CHECK FADE MANAGER
        // --------------------------------------------------------

        if (fadeManage == null)
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "RFadeManager is NOT assigned!"
            );

            return;
        }


        // --------------------------------------------------------
        // CHECK SCENE NAME
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(_sceneToLoad))
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "Scene to load is empty!"
            );

            return;
        }


        DebugMessage(
            "Starting scene load coroutine."
        );


        // --------------------------------------------------------
        // START COROUTINE
        // --------------------------------------------------------

        StartCoroutine(
            LoadSceneFade()
        );
    }


    // ============================================================
    // SETTINGS BUTTON
    // ============================================================

    public void SettingsButtonClicked(
        SettingButtons buttonClicked
    )
    {
        switch (buttonClicked)
        {
            case SettingButtons.back:

                ReturnToMainMenu();

                break;
        }
    }


    // ============================================================
    // SETTINGS
    // ============================================================

    public void SettingsClicked()
    {
        DebugMessage(
            "Opening Settings."
        );


        OpenMenu(
            _SettingsContainer
        );
    }


    // ============================================================
    // ALMANAC BUTTON
    // ============================================================

    public void AlmanacButtonClicked(
        AlmanacButtons buttonClicked
    )
    {
        switch (buttonClicked)
        {
            case AlmanacButtons.back:

                ReturnToMainMenu();

                break;
        }
    }


    // ============================================================
    // ALMANAC
    // ============================================================

    public void AlmanacClicked()
    {
        DebugMessage(
            "Opening Almanac."
        );


        OpenMenu(
            _AlmanacContainer
        );
    }


    // ============================================================
    // AWARDS
    // ============================================================

    public void AwardsClicked()
    {
        DebugMessage(
            "Opening Awards."
        );


        OpenMenu(
            _AwardsContainer
        );
    }


    // ============================================================
    // DEBUG MESSAGE
    // ============================================================

    private void DebugMessage(
        string message
    )
    {
        if (_debugMode)
        {
            Debug.Log(
                "[MainMenuManager] " +
                message
            );
        }
    }


    // ============================================================
    // QUIT GAME
    // ============================================================

    public void QuitGame()
    {
        DebugMessage(
            "Quit Game clicked."
        );


#if UNITY_EDITOR

        UnityEditor.EditorApplication.ExitPlaymode();

#else

        Application.Quit();

#endif
    }


    // ============================================================
    // OPEN MENU
    // ============================================================

    public void OpenMenu(
        GameObject menuToOpen
    )
    {
        if (menuToOpen == null)
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "OpenMenu received a NULL menu."
            );

            return;
        }


        // --------------------------------------------------------
        // MAIN MENU
        // --------------------------------------------------------

        if (_MainMenuContainer != null)
        {
            _MainMenuContainer.SetActive(
                menuToOpen == _MainMenuContainer
            );
        }
        else
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "_MainMenuContainer is NOT assigned!"
            );
        }


        // --------------------------------------------------------
        // ALMANAC
        // --------------------------------------------------------

        if (_AlmanacContainer != null)
        {
            _AlmanacContainer.SetActive(
                menuToOpen == _AlmanacContainer
            );
        }
        else
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "_AlmanacContainer is NOT assigned!"
            );
        }


        // --------------------------------------------------------
        // SETTINGS
        // --------------------------------------------------------

        if (_SettingsContainer != null)
        {
            _SettingsContainer.SetActive(
                menuToOpen == _SettingsContainer
            );
        }
        else
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "_SettingsContainer is NOT assigned!"
            );
        }


        // --------------------------------------------------------
        // AWARDS
        // --------------------------------------------------------

        if (_AwardsContainer != null)
        {
            _AwardsContainer.SetActive(
                menuToOpen == _AwardsContainer
            );
        }
        else
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "_AwardsContainer is NOT assigned!"
            );
        }


        DebugMessage(
            "Opened menu: " +
            menuToOpen.name
        );
    }


    // ============================================================
    // LOAD SCENE WITH FADE
    // ============================================================

    private IEnumerator LoadSceneFade()
    {
        DebugMessage(
            "LoadSceneFade coroutine started."
        );


        // --------------------------------------------------------
        // CHECK FADE MANAGER
        // --------------------------------------------------------

        if (fadeManage == null)
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "Fade Manager disappeared before coroutine started."
            );

            yield break;
        }


        // --------------------------------------------------------
        // FADE
        // --------------------------------------------------------

        fadeManage.DoFade(
            0f,
            1.5f,
            1f,
            0f
        );


        DebugMessage(
            "Fade started. Waiting 1 second..."
        );


        // --------------------------------------------------------
        // WAIT
        // --------------------------------------------------------

        yield return new WaitForSeconds(
            1f
        );


        // --------------------------------------------------------
        // CHECK SCENE NAME
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(_sceneToLoad))
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "Scene name is empty."
            );

            yield break;
        }


        DebugMessage(
            "Loading scene: " +
            _sceneToLoad
        );


        // --------------------------------------------------------
        // LOAD SCENE
        // --------------------------------------------------------

        SceneManager.LoadScene(
            _sceneToLoad
        );
    }


    // ============================================================
    // CHECK REFERENCES
    // ============================================================

    private void CheckReferences()
    {
        Debug.Log(
            "========== MainMenuManager References =========="
        );


        // --------------------------------------------------------
        // FADE MANAGER
        // --------------------------------------------------------

        if (fadeManage == null)
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "fadeManage is NOT assigned!"
            );
        }
        else
        {
            Debug.Log(
                "[MainMenuManager] fadeManage = " +
                fadeManage.gameObject.name
            );
        }


        // --------------------------------------------------------
        // MAIN MENU
        // --------------------------------------------------------

        if (_MainMenuContainer == null)
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "_MainMenuContainer is NOT assigned!"
            );
        }
        else
        {
            Debug.Log(
                "[MainMenuManager] Main Menu = " +
                _MainMenuContainer.name
            );
        }


        // --------------------------------------------------------
        // SETTINGS
        // --------------------------------------------------------

        if (_SettingsContainer == null)
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "_SettingsContainer is NOT assigned!"
            );
        }
        else
        {
            Debug.Log(
                "[MainMenuManager] Settings = " +
                _SettingsContainer.name
            );
        }


        // --------------------------------------------------------
        // ALMANAC
        // --------------------------------------------------------

        if (_AlmanacContainer == null)
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "_AlmanacContainer is NOT assigned!"
            );
        }
        else
        {
            Debug.Log(
                "[MainMenuManager] Almanac = " +
                _AlmanacContainer.name
            );
        }


        // --------------------------------------------------------
        // AWARDS
        // --------------------------------------------------------

        if (_AwardsContainer == null)
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "_AwardsContainer is NOT assigned!"
            );
        }
        else
        {
            Debug.Log(
                "[MainMenuManager] Awards = " +
                _AwardsContainer.name
            );
        }


        // --------------------------------------------------------
        // SCENE
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(_sceneToLoad))
        {
            Debug.LogError(
                "[MainMenuManager] " +
                "_sceneToLoad is EMPTY!"
            );
        }
        else
        {
            Debug.Log(
                "[MainMenuManager] Scene to load = " +
                _sceneToLoad
            );
        }


        Debug.Log(
            "================================================"
        );
    }
}