using UnityEngine;

public class SettingsButtonManager : MonoBehaviour
{
    [SerializeField]
    private MainMenuManager.SettingButtons _Buttontype;


    public void ButtonClicked()
    {
        Debug.Log(
            "[SettingsButtonManager] Button clicked: " +
            _Buttontype
        );


        MainMenuManager manager =
            MainMenuManager.Instance;


        if (manager == null)
        {
            Debug.LogError(
                "[SettingsButtonManager] " +
                "MainMenuManager does not exist!"
            );

            return;
        }


        Debug.Log(
            "[SettingsButtonManager] " +
            "Found MainMenuManager: " +
            manager.gameObject.name
        );


        manager.SettingsButtonClicked(
            _Buttontype
        );
    }
}