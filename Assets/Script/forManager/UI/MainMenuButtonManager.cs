using UnityEngine;

public class MainMenuButtonManager : MonoBehaviour
{
    [SerializeField] private MainMenuManager.MainMenuButton _Buttontype;

    public void ButtonClicked()
    {
        MainMenuManager._.MainMenuButtonClicked(_Buttontype);
    }
}
