using UnityEngine;
using UnityEngine.UI;

// Attach this to the Settings panel GameObject in MainMenu (or any scene
// that has the Music/Sound toggles). It runs every time this scene loads
// and hands the CURRENT toggle references to the persistent GlobalAudioToggle
// manager, so the manager never ends up listening to a stale/destroyed Toggle.
public class SettingsToggleRegistrar : MonoBehaviour
{
    [Header("Toggles in this scene")]
    public Toggle musicToggle;
    public Toggle soundToggle;

    void Start()
    {
        if (GlobalAudioToggle.Instance == null)
        {
            Debug.LogWarning("[SettingsToggleRegistrar] GlobalAudioToggle.Instance not found. " +
                              "Make sure the persistent AudioManager exists before this scene loads.");
            return;
        }

        GlobalAudioToggle.Instance.RegisterMusicToggle(musicToggle);
        GlobalAudioToggle.Instance.RegisterSoundToggle(soundToggle);
    }
}