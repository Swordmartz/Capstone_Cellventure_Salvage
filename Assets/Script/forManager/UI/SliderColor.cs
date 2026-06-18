using UnityEngine;
using UnityEngine.UI;

public class SliderFillColor : MonoBehaviour
{
    [Header("References")]
    public Slider slider;
    public Image fillImage;

    [Header("Colors")]
    public Color lowColor = Color.red;
    public Color midColor = Color.yellow;
    public Color highColor = Color.green;

    void Start()
    {
        if (slider != null)
            slider.onValueChanged.AddListener(UpdateFillColor);

        UpdateFillColor(slider != null ? slider.value : 0f);
    }

    void UpdateFillColor(float value)
    {
        if (slider == null || fillImage == null) return;

        float normalized = Mathf.InverseLerp(slider.minValue, slider.maxValue, value);

        Color targetColor;

        if (normalized < 0.5f)
        {
            // Blend red -> yellow across the first half
            targetColor = Color.Lerp(lowColor, midColor, normalized / 0.5f);
        }
        else
        {
            // Blend yellow -> green across the second half
            targetColor = Color.Lerp(midColor, highColor, (normalized - 0.5f) / 0.5f);
        }

        fillImage.color = targetColor;
    }
}