using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class LoadingMenu : MonoBehaviour
{
    public static LoadingMenu Instance;
    public GameObject loadingScreen;
    public Slider loadingSlider;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void SwitchScene(int id)
    {
        loadingScreen.SetActive(true);
        loadingSlider.value = 0;
        StartCoroutine(SwitchtiSceneAsync(id));
    }

    IEnumerator SwitchtiSceneAsync (int id)
    {
        AsyncOperation AsyncLoad = SceneManager.LoadSceneAsync(id);
        while (!AsyncLoad.isDone)
        {
            loadingSlider.value = AsyncLoad.progress;
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);
        loadingScreen.gameObject.SetActive(false);
    }
}

