using UnityEngine;

public class AlmanacButtonManager : MonoBehaviour
{
    [SerializeField] MainMenuManager.AlmanacButtons _Buttontype;

    public void ButtonClicked()
    {
        MainMenuManager._.AlmanacButtonClicked(_Buttontype);
    }
}
