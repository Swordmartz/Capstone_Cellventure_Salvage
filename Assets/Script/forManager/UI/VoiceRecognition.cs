using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Vosk;

public class VoiceCommandManager : MonoBehaviour
{
    public static VoiceCommandManager Instance;

    [Header("Main Menu Scene")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Main Menu Button Object Names")]
    public string startButtonName = "StartButton";
    public string settingsButtonName = "SettingsButton";
    public string almanacButtonName = "AlmanacButton";
    public string quitButtonName = "QuitButton";
    public string achievementButtonName = "AchievementButton";
    public string skipButtonName = "SkipButton";

    [Header("Gameplay UI Object Names")]
    public string pausePanelName = "PausePanel";

    [Header("Vosk Settings")]
    public string modelName = "vosk-model-small-en-us-0.15";
    public int sampleRate = 16000;

    private Button startButton;
    private Button settingsButton;
    private Button almanacButton;
    private Button quitButton;
    private Button achievementButton;
    private Button skipButton;

    private GameObject pausePanel;

    private VoskRecognizer recognizer;
    private Model model;
    private AudioClip micClip;

    private bool isRunning = false;
    private bool modelReady = false;
    private bool isPaused = false;

    private string selectedMicName = null;
    private string lastPartial = "";
    private int lastSamplePos = 0;

    private float[] floatBuffer;
    private short[] shortBuffer;
    private byte[] byteBuffer;

    private Dictionary<string, System.Action> commands;

    // ──────────────────────────────────────
    //  LIFECYCLE
    // ──────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        Debug.Log("[Vosk] Starting up...");

        RegisterCommands();
        RefreshSceneReferences();

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.Log("[Vosk] Requesting microphone permission...");
            Permission.RequestUserPermission(Permission.Microphone);
            StartCoroutine(WaitForPermissionThenInit());
        }
        else
        {
            Debug.Log("[Vosk] Microphone permission already granted.");
            StartCoroutine(InitVosk());
        }
    }

    private void Update()
    {
        ProcessMicrophoneAudio();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[Vosk] Scene loaded: " + scene.name);

        Time.timeScale = 1f;
        isPaused = false;

        StartCoroutine(RefreshSceneReferencesNextFrame());

        if (modelReady && !isRunning)
            StartCoroutine(RestartMicrophoneOnMainThread());
    }

    private IEnumerator RefreshSceneReferencesNextFrame()
    {
        yield return null;
        RefreshSceneReferences();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            Debug.Log("[Vosk] App paused — stopping microphone.");
            StopMicrophone();
        }
        else
        {
            Debug.Log("[Vosk] App resumed — restarting microphone.");

            if (modelReady)
                StartCoroutine(RestartMicrophoneOnMainThread());
            else
                Debug.LogWarning("[Vosk] Recognizer not ready yet — skipping restart.");
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[Vosk] App closing — shutting down Vosk.");
        ShutdownVosk();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        ShutdownVosk();

        Debug.Log("[Vosk] VoiceCommandManager destroyed.");
    }

    private void ShutdownVosk()
    {
        StopMicrophone();

        recognizer?.Dispose();
        recognizer = null;

        model?.Dispose();
        model = null;

        modelReady = false;

        Debug.Log("[Vosk] Vosk shut down cleanly.");
    }

    // ──────────────────────────────────────
    //  INITIALIZATION
    // ──────────────────────────────────────

    private IEnumerator WaitForPermissionThenInit()
    {
        yield return new WaitUntil(() =>
            Permission.HasUserAuthorizedPermission(Permission.Microphone));

        Debug.Log("[Vosk] Permission granted — initializing...");
        StartCoroutine(InitVosk());
    }

    private IEnumerator InitVosk()
    {
        string modelPath = System.IO.Path.Combine(
            Application.persistentDataPath,
            modelName
        );

        if (!System.IO.Directory.Exists(modelPath))
        {
            Debug.Log("[Vosk] Copying model to persistentDataPath...");
            yield return StartCoroutine(CopyModelFromStreamingAssets(modelPath));
        }
        else
        {
            Debug.Log("[Vosk] Model already exists at: " + modelPath);
        }

        bool modelLoaded = false;
        string loadError = "";

        System.Threading.Thread loadThread = new System.Threading.Thread(() =>
        {
            try
            {
                model = new Model(modelPath);
                recognizer = new VoskRecognizer(model, sampleRate);

                recognizer.SetMaxAlternatives(0);
                recognizer.SetWords(false);

                modelLoaded = true;
            }
            catch (System.Exception e)
            {
                loadError = e.Message;
            }
        });

        loadThread.IsBackground = true;
        loadThread.Start();

        yield return new WaitUntil(() => modelLoaded || !string.IsNullOrEmpty(loadError));

        if (!string.IsNullOrEmpty(loadError))
        {
            Debug.LogError("[Vosk] Failed to load model: " + loadError);
            yield break;
        }

        Debug.Log("[Vosk] Model loaded successfully.");

        modelReady = true;
        StartMicrophone();
    }

    private IEnumerator CopyModelFromStreamingAssets(string destPath)
    {
        System.IO.Directory.CreateDirectory(destPath);

        string srcPath = System.IO.Path.Combine(
            Application.streamingAssetsPath,
            modelName
        );

        string fileListUrl = srcPath + "/filelist.txt";

        using (UnityEngine.Networking.UnityWebRequest www =
            UnityEngine.Networking.UnityWebRequest.Get(fileListUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError("[Vosk] Could not get filelist.txt: " + www.error);
                yield break;
            }

            string[] files = www.downloadHandler.text.Split(
                new char[] { '\n', '\r' },
                System.StringSplitOptions.RemoveEmptyEntries
            );

            Debug.Log("[Vosk] Copying " + files.Length + " files...");

            foreach (string file in files)
            {
                string trimmed = file.Trim();

                if (string.IsNullOrEmpty(trimmed))
                    continue;

                string src = srcPath + "/" + trimmed;
                string dest = System.IO.Path.Combine(destPath, trimmed);

                string dir = System.IO.Path.GetDirectoryName(dest);

                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                using (UnityEngine.Networking.UnityWebRequest fileWww =
                    UnityEngine.Networking.UnityWebRequest.Get(src))
                {
                    yield return fileWww.SendWebRequest();

                    if (fileWww.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        System.IO.File.WriteAllBytes(dest, fileWww.downloadHandler.data);
                    }
                    else
                    {
                        Debug.LogError("[Vosk] Failed to copy: " + trimmed + " — " + fileWww.error);
                    }
                }
            }
        }

        Debug.Log("[Vosk] Model copy complete.");
    }

    // ──────────────────────────────────────
    //  MICROPHONE
    // ──────────────────────────────────────

    private void StartMicrophone()
    {
        if (!modelReady || recognizer == null)
        {
            Debug.LogWarning("[Vosk] Cannot start microphone because recognizer is not ready.");
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[Vosk] No microphone found!");
            return;
        }

        if (isRunning)
        {
            Debug.LogWarning("[Vosk] Microphone already running.");
            return;
        }

        selectedMicName = Microphone.devices[0];

        Debug.Log("[Vosk] Using microphone: " + selectedMicName);

        micClip = Microphone.Start(
            selectedMicName,
            true,
            1,
            sampleRate
        );

        int bufferSize = sampleRate / 10;

        floatBuffer = new float[bufferSize];
        shortBuffer = new short[bufferSize];
        byteBuffer = new byte[bufferSize * 2];

        lastSamplePos = 0;
        lastPartial = "";

        isRunning = true;

        Debug.Log("[Vosk] Continuous offline listening started.");
    }

    private void StopMicrophone()
    {
        isRunning = false;

        if (!string.IsNullOrEmpty(selectedMicName) && Microphone.IsRecording(selectedMicName))
            Microphone.End(selectedMicName);

        micClip = null;

        Debug.Log("[Vosk] Microphone stopped.");
    }

    private IEnumerator RestartMicrophoneOnMainThread()
    {
        yield return null;

        if (!isRunning)
            StartMicrophone();
    }

    private void ProcessMicrophoneAudio()
    {
        if (!isRunning)
            return;

        if (micClip == null)
            return;

        if (recognizer == null)
            return;

        if (floatBuffer == null || shortBuffer == null || byteBuffer == null)
            return;

        int bufferSize = floatBuffer.Length;

        int currentPos = Microphone.GetPosition(selectedMicName);

        if (currentPos < 0)
            return;

        int available = currentPos - lastSamplePos;

        if (available < 0)
            available += micClip.samples;

        if (available < bufferSize)
            return;

        micClip.GetData(floatBuffer, lastSamplePos);

        lastSamplePos = (lastSamplePos + bufferSize) % micClip.samples;

        for (int i = 0; i < bufferSize; i++)
        {
            float sample = Mathf.Clamp(floatBuffer[i], -1f, 1f);
            shortBuffer[i] = (short)(sample * 32767);
        }

        System.Buffer.BlockCopy(
            shortBuffer,
            0,
            byteBuffer,
            0,
            byteBuffer.Length
        );

        if (recognizer.AcceptWaveform(byteBuffer, byteBuffer.Length))
        {
            string result = recognizer.Result();

            if (!string.IsNullOrEmpty(result))
                ProcessResult(result);
        }
        else
        {
            string partial = recognizer.PartialResult();

            if (!string.IsNullOrEmpty(partial) && partial != lastPartial)
            {
                lastPartial = partial;
                Debug.Log("[Vosk] Partial: " + partial);
            }
        }
    }

    // ──────────────────────────────────────
    //  RESULT PROCESSING
    // ──────────────────────────────────────

    private void ProcessResult(string json)
    {
        string lower = json.ToLower();

        int textIndex = lower.IndexOf("\"text\"");

        if (textIndex < 0)
            return;

        int start = lower.IndexOf("\"", textIndex + 6) + 1;
        int end = lower.IndexOf("\"", start);

        if (start <= 0 || end <= 0)
            return;

        string heard = lower.Substring(start, end - start).Trim();

        if (string.IsNullOrEmpty(heard))
            return;

        Debug.Log("[Vosk] HEARD: \"" + heard + "\"");

        bool matched = false;

        foreach (var command in commands)
        {
            if (heard.Contains(command.Key))
            {
                Debug.Log("[Vosk] MATCHED: \"" + command.Key + "\"");
                command.Value.Invoke();
                matched = true;
                break;
            }
        }

        if (!matched)
            Debug.LogWarning("[Vosk] No match for: \"" + heard + "\"");
    }

    // ──────────────────────────────────────
    //  COMMANDS
    // ──────────────────────────────────────

    private void RegisterCommands()
    {
        commands = new Dictionary<string, System.Action>
        {
            { "start",        () => InvokeButton(startButton,       "Start") },
            { "adventure",    () => InvokeButton(startButton,       "Start") },

            { "settings",     () => InvokeButton(settingsButton,    "Settings") },
            { "options",      () => InvokeButton(settingsButton,    "Settings") },

            { "almanac",      () => InvokeButton(almanacButton,     "Almanac") },
            { "book",         () => InvokeButton(almanacButton,     "Almanac") },

            { "achievements", () => InvokeButton(achievementButton, "Achievements") },
            { "achievement",  () => InvokeButton(achievementButton, "Achievements") },
            { "awards",       () => InvokeButton(achievementButton, "Achievements") },

            { "skip",         () => InvokeButton(skipButton,        "Skip") },
            { "next",         () => InvokeButton(skipButton,        "Skip") },

            { "quit",         () => InvokeButton(quitButton,        "Quit") },
            { "exit",         () => InvokeButton(quitButton,        "Quit") },

            { "pause",        PauseGame },
            { "stop",         PauseGame },

            { "resume",       ResumeGame },
            { "continue",     ResumeGame },

            { "restart",      RestartScene },
            { "retry",        RestartScene },

            { "main menu",    GoToMainMenu },
            { "menu",         GoToMainMenu },
            { "home",         GoToMainMenu },
        };

        Debug.Log("[Vosk] " + commands.Count + " commands registered.");
    }

    // ──────────────────────────────────────
    //  GAMEPLAY VOICE ACTIONS
    // ──────────────────────────────────────

    public void PauseGame()
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            Debug.Log("[Vosk] Pause ignored because player is in main menu.");
            return;
        }

        if (isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        RefreshPausePanelReference();

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            Debug.Log("[Vosk] Pause panel shown: " + pausePanel.name);
        }
        else
        {
            Debug.LogWarning("[Vosk] Pause panel not found. Check Pause Panel Name: " + pausePanelName);
        }

        Debug.Log("[Vosk] Game paused.");
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        RefreshPausePanelReference();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Debug.Log("[Vosk] Game resumed.");
    }

    public void RestartScene()
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            Debug.Log("[Vosk] Restart ignored because player is in main menu.");
            return;
        }

        Time.timeScale = 1f;
        isPaused = false;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);

        Debug.Log("[Vosk] Restarting scene.");
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;

        SceneManager.LoadScene(mainMenuSceneName);

        Debug.Log("[Vosk] Going to main menu.");
    }

    // ──────────────────────────────────────
    //  SCENE REFERENCES
    // ──────────────────────────────────────

    private void RefreshSceneReferences()
    {
        RefreshMainMenuButtonReferences();
        RefreshPausePanelReference();
    }

    private void RefreshMainMenuButtonReferences()
    {
        startButton = FindButtonByObjectName(startButtonName);
        settingsButton = FindButtonByObjectName(settingsButtonName);
        almanacButton = FindButtonByObjectName(almanacButtonName);
        quitButton = FindButtonByObjectName(quitButtonName);
        achievementButton = FindButtonByObjectName(achievementButtonName);
        skipButton = FindButtonByObjectName(skipButtonName);

        Debug.Log("[Vosk] Scene button references refreshed.");
    }

    private void RefreshPausePanelReference()
    {
        pausePanel = FindObjectInActiveSceneByName(pausePanelName);
    }

    private Button FindButtonByObjectName(string objectName)
    {
        GameObject obj = FindObjectInActiveSceneByName(objectName);

        if (obj == null)
            return null;

        return obj.GetComponent<Button>();
    }

    private GameObject FindObjectInActiveSceneByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        Scene activeScene = SceneManager.GetActiveScene();

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform t in allTransforms)
        {
            if (t == null)
                continue;

            if (t.gameObject.scene != activeScene)
                continue;

            if (t.name == objectName)
                return t.gameObject;
        }

        return null;
    }

    // ──────────────────────────────────────
    //  HELPERS
    // ──────────────────────────────────────

    private void InvokeButton(Button btn, string name)
    {
        if (btn != null && btn.gameObject.activeInHierarchy)
        {
            Debug.Log("[Vosk] Invoking: " + name);
            btn.onClick.Invoke();
        }
        else
        {
            Debug.LogWarning("[Vosk] Button '" + name + "' is null or inactive — skipping.");
        }
    }
}