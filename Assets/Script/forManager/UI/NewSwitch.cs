using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BGMSwitchButton : MonoBehaviour
{
    [Header("Button")]
    public Button switchButton;

    [Header("Switch Knob")]
    public RectTransform knob;
    public Vector2 knobOnPosition;
    public Vector2 knobOffPosition;
    public float slideDuration = 0.2f;

    [Header("Background Music")]
    public AudioSource[] bgmSources;

    [Header("Default State")]
    public bool startOn = true;

    private bool isOn;
    private Coroutine slideCoroutine;

    private void Start()
    {
        isOn = startOn;

        if (switchButton != null)
            switchButton.onClick.AddListener(ToggleSwitch);

        ApplyStateInstant();
    }

    private void ToggleSwitch()
    {
        isOn = !isOn;

        ApplyAudioState();
        AnimateKnob();
    }

    private void ApplyAudioState()
    {
        foreach (AudioSource source in bgmSources)
        {
            if (source != null)
                source.mute = !isOn;
        }
    }

    private void AnimateKnob()
    {
        if (knob == null)
            return;

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        Vector2 targetPosition = isOn ? knobOnPosition : knobOffPosition;
        slideCoroutine = StartCoroutine(SlideKnob(targetPosition));
    }

    private IEnumerator SlideKnob(Vector2 targetPosition)
    {
        Vector2 startPosition = knob.anchoredPosition;
        float timer = 0f;

        while (timer < slideDuration)
        {
            timer += Time.deltaTime;

            float t = timer / slideDuration;
            t = t * t * (3f - 2f * t); // smooth movement

            knob.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        knob.anchoredPosition = targetPosition;
    }

    private void ApplyStateInstant()
    {
        ApplyAudioState();

        if (knob != null)
            knob.anchoredPosition = isOn ? knobOnPosition : knobOffPosition;
    }
}