using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GlobalAudioToggle : MonoBehaviour
{
    public static GlobalAudioToggle Instance { get; private set; }

    private Toggle musicToggle;
    private const string MusicPrefKey = "MusicEnabled";
    private const string MusicTag = "Music";

    private Toggle soundToggle;
    private const string SoundPrefKey = "SoundEnabled";
    private const string SoundTag = "Sound";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        ApplyMute(MusicTag, IsMusicOn());
        ApplyMute(SoundTag, IsSoundOn());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-apply saved mute state to whatever AudioSources exist in the new scene.
        // Toggle re-registration is handled separately by SettingsToggleRegistrar,
        // which calls RegisterMusicToggle/RegisterSoundToggle below when it wakes up
        // in the new scene (e.g. when MainMenu loads).
        ApplyMute(MusicTag, IsMusicOn());
        ApplyMute(SoundTag, IsSoundOn());
    }

    // Called by SettingsToggleRegistrar (placed on the Settings panel) every time
    // a scene containing the Settings UI loads. This guarantees we always have a
    // reference to the CURRENT, alive Toggle object, never a stale/destroyed one.
    public void RegisterMusicToggle(Toggle toggle)
    {
        if (musicToggle != null)
            musicToggle.onValueChanged.RemoveListener(OnMusicChanged);

        musicToggle = toggle;

        if (musicToggle != null)
        {
            musicToggle.SetIsOnWithoutNotify(IsMusicOn());
            musicToggle.onValueChanged.AddListener(OnMusicChanged);
        }
    }

    public void RegisterSoundToggle(Toggle toggle)
    {
        if (soundToggle != null)
            soundToggle.onValueChanged.RemoveListener(OnSoundChanged);

        soundToggle = toggle;

        if (soundToggle != null)
        {
            soundToggle.SetIsOnWithoutNotify(IsSoundOn());
            soundToggle.onValueChanged.AddListener(OnSoundChanged);
        }
    }

    private bool IsMusicOn() => PlayerPrefs.GetInt(MusicPrefKey, 1) == 1;
    private bool IsSoundOn() => PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;

    private void OnMusicChanged(bool isOn)
    {
        ApplyMute(MusicTag, isOn);
        PlayerPrefs.SetInt(MusicPrefKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnSoundChanged(bool isOn)
    {
        ApplyMute(SoundTag, isOn);
        PlayerPrefs.SetInt(SoundPrefKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyMute(string tag, bool isOn)
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);

        foreach (GameObject obj in taggedObjects)
        {
            AudioSource source = obj.GetComponent<AudioSource>();
            if (source != null)
                source.mute = !isOn;
        }
    }
}