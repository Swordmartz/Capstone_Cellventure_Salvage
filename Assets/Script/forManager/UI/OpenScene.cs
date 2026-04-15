using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private int ID;

    public void OpenScene()
    {
        LoadingMenu.Instance.SwitchScene(ID);
    }
}
