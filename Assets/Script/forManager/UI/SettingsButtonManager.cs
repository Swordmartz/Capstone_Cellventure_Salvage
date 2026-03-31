using UnityEngine;

public class SettingsButtonManager : MonoBehaviour
{
    [SerializeField] MainMenuManager.SettingButtons _Buttontype;

    public void ButtonClicked()
    {
        MainMenuManager._.SettingsButtonClicked(_Buttontype);
    }
}
