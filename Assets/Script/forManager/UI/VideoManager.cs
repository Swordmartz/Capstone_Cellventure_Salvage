using UnityEngine;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    [Header("UI")]
    public GameObject mainUI;     // All other UI
    public GameObject skipButton; // Skip button only

    void Start()
    {
        // Disable all UI except skip button
        if (mainUI != null) mainUI.SetActive(false);
        if (skipButton != null) skipButton.SetActive(true);

        // Prepare video to avoid delay
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += PlayVideo;

        // Detect when video ends automatically
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    void PlayVideo(VideoPlayer vp)
    {
        videoPlayer.Play();
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        StopVideo();
    }

    // Called by Skip Button
    public void SkipVideo()
    {
        // Hide skip button immediately
        if (skipButton != null) skipButton.SetActive(false);

        StopVideo();
    }

    void StopVideo()
    {
        // Pause first
        videoPlayer.Pause();

        // Mute all audio tracks
        for (ushort i = 0; i < videoPlayer.audioTrackCount; i++)
        {
            videoPlayer.SetDirectAudioMute(i, true);
            videoPlayer.SetDirectAudioVolume(i, 0f);
            videoPlayer.EnableAudioTrack(i, false);
        }

        // Stop video
        videoPlayer.Stop();

        // Bring back main UI
        if (mainUI != null) mainUI.SetActive(true);

        Debug.Log("Video stopped/skipped, audio muted, UI restored");
    }
}