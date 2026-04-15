using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FFadeManager : MonoBehaviour
{
    [SerializeField]private Image _fadeImage;
    [SerializeField] private CanvasGroup _fadeCanvasGroup;
    [SerializeField] private bool _autoFadeIn;
    [SerializeField] private float AutStartA;
    [SerializeField] private float AutEndA;
    [SerializeField] private float AutDuration;
    [SerializeField] private float AutdelayBfade;
    private Coroutine _fadeCo;
    private void Awake()
    {
        if (_autoFadeIn)
        {
            DoFade(AutStartA, AutEndA, AutDuration, AutdelayBfade);
        }
    }
    public void DoFade(float StartA, float EndA, float Duration, float delayBfade)
    {
        if (_fadeCo != null)
        {
            StopCoroutine(_fadeCo);
        }
        _fadeCo = StartCoroutine(AnimFade(StartA, EndA, Duration, delayBfade));
    }

    private IEnumerator AnimFade(float StartA, float EndA, float Duration, float delayBfade)
    {
        _fadeImage.enabled = true;
        _fadeCanvasGroup.alpha = StartA;
        yield return null;
        yield return new WaitForSeconds(delayBfade);
        float TimeElapse = 0;
        while (TimeElapse < Duration)
        {
            TimeElapse += Time.deltaTime;
            float fadeProgress = TimeElapse/Duration;
            fadeProgress = Mathf.Clamp01(fadeProgress);
            _fadeCanvasGroup.alpha = Mathf.Lerp(StartA, EndA, fadeProgress);
            yield return null;
        }

        _fadeCanvasGroup.alpha = EndA;

        if (EndA <= 0)
        {
            _fadeImage.enabled = false;
        }
    }
}
