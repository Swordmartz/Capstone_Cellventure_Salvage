using UnityEngine;
using UnityEngine.UI;

public class SliderTimer : MonoBehaviour
{
    public Slider slider;
    public Button completeButton;
    public float maxValue = 10f;

    [Tooltip("How much charge is added per second automatically.")]
    public float regenRate = 1f;  // ← change this in the Inspector

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
            sliderValue += regenRate;  // ← uses regenRate instead of hardcoded 1f
            slider.value = sliderValue;
        }

        if (sliderValue >= maxValue)
        {
            completeButton.gameObject.SetActive(true);
        }
    }

    public void ConsumeBar()
    {
        sliderValue = 0f;
        elapsed = 0f;
        slider.value = 0f;
        completeButton.gameObject.SetActive(false);
    }

    public void AddCharge(float amount)
    {
        sliderValue = Mathf.Min(sliderValue + amount, maxValue);
        slider.value = sliderValue;

        if (sliderValue >= maxValue)
            completeButton.gameObject.SetActive(true);
    }
}