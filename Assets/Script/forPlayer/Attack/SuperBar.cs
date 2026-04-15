using UnityEngine;
using UnityEngine.UI;

public class SliderTimer : MonoBehaviour
{
    public Slider slider;
    public Button completeButton;
    public float maxValue = 10f;

    private float elapsed = 0f;
    private float sliderValue = 0f;

    public bool IsFull => sliderValue >= maxValue;

    void Start()
    {
        slider.minValue = 0f;
        slider.maxValue = maxValue;
        slider.value = 0f;

        completeButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (sliderValue >= maxValue) return;

        elapsed += Time.deltaTime;

        while (elapsed >= 1f && sliderValue < maxValue)
        {
            elapsed -= 1f;
            sliderValue += 1f;
            slider.value = sliderValue;
        }

        if (sliderValue >= maxValue)
        {
            completeButton.gameObject.SetActive(true);
        }
    }

    // Called by SuperMove after a successful activation
    public void ConsumeBar()
    {
        sliderValue = 0f;
        elapsed = 0f;
        slider.value = 0f;
        completeButton.gameObject.SetActive(false);
    }
}