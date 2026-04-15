using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroVideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Button skipButton;
    public RawImage videoImage;
    public GameObject mainGameObject;

    // Static = resets when app fully closes, survives scene reloads
    private static bool hasPlayedThisSession = false;

    void Start()
    {
        if (hasPlayedThisSession)
        {
            SkipToGame();
            return;
        }

        // First time this session — play the video
        videoPlayer.Play();
        skipButton.onClick.AddListener(SkipVideo);
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void SkipVideo()
    {
        StopVideoAndHideUI();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        StopVideoAndHideUI();
    }

    void StopVideoAndHideUI()
    {
        hasPlayedThisSession = true;  // Remember for this session only

        videoPlayer.Stop();

        if (videoImage != null)
            videoImage.enabled = false;

        skipButton.gameObject.SetActive(false);
        mainGameObject.gameObject.SetActive(true);
    }

    void SkipToGame()
    {
        videoPlayer.gameObject.SetActive(false);

        if (videoImage != null)
            videoImage.enabled = false;

        skipButton.gameObject.SetActive(false);
        mainGameObject.gameObject.SetActive(true);
    }
}