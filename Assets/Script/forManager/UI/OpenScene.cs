using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void OpenScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
