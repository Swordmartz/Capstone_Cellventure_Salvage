using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlainOpenScene : MonoBehaviour
{
    [SerializeField] private RFadeManager fadeManager;
    [SerializeField] private string scenetoload;

    public void OpentoScene()
    {
        StartCoroutine(LoadSceneWithFade());
    }
    IEnumerator LoadSceneWithFade()
    {
        fadeManager.DoFade(0, 1f, .2f, 0f);
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(scenetoload);
    }
}
