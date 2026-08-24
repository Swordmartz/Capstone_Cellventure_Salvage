using UnityEngine;

public class AlmanacButtonManager : MonoBehaviour
{
    [SerializeField]
    private MainMenuManager.AlmanacButtons _Buttontype;


    public void ButtonClicked()
    {
        Debug.Log(
            "[AlmanacButtonManager] Button clicked: " +
            _Buttontype
        );


        MainMenuManager manager =
            MainMenuManager.Instance;


        if (manager == null)
        {
            Debug.LogError(
                "[AlmanacButtonManager] " +
                "MainMenuManager does not exist!"
            );

            return;
        }


        Debug.Log(
            "[AlmanacButtonManager] " +
            "Found MainMenuManager: " +
            manager.gameObject.name
        );


        manager.AlmanacButtonClicked(
            _Buttontype
        );
    }
}