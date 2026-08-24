using UnityEngine;

public class MainMenuButtonManager : MonoBehaviour
{
    [SerializeField]
    private MainMenuManager.MainMenuButton _Buttontype;


    public void ButtonClicked()
    {
        Debug.Log(
            "[MainMenuButtonManager] Button clicked: " +
            _Buttontype
        );


        MainMenuManager manager =
            MainMenuManager.Instance;


        if (manager == null)
        {
            Debug.LogError(
                "[MainMenuButtonManager] " +
                "MainMenuManager does not exist!"
            );

            return;
        }


        Debug.Log(
            "[MainMenuButtonManager] " +
            "Found MainMenuManager: " +
            manager.gameObject.name
        );


        manager.MainMenuButtonClicked(
            _Buttontype
        );
    }
}