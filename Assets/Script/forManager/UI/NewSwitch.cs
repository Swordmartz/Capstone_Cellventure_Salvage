using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Toggle))]
public class AnimatedToggleSwitch : MonoBehaviour
{
    [Header("References")]
    public Toggle toggle;
    public RectTransform knob;
    public Image trackImage;
    public TextMeshProUGUI label; // optional, leave empty if not using a label

    [Header("Track Colors")]
    public Color onColor = new Color(0.36f, 0.78f, 0.42f);   // green
    public Color offColor = new Color(0.18f, 0.22f, 0.32f);  // dark navy

    [Header("Knob Position (local X)")]
    public float knobOnX = 13f;
    public float knobOffX = -13f;

    [Header("Animation")]
    public float animDuration = 0.15f;

    [Header("Label Text")]
    public string onText = "ON";
    public string offText = "OFF";

    private Coroutine animCoroutine;

    void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();

        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    void Start()
    {
        // Snap to correct initial state without animating
        ApplyVisualsInstant(toggle.isOn);
    }

    void OnToggleChanged(bool isOn)
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        animCoroutine = StartCoroutine(AnimateToggle(isOn));
    }

    private System.Collections.IEnumerator AnimateToggle(bool isOn)
    {
        float elapsed = 0f;

        Vector2 startPos = knob.anchoredPosition;
        Vector2 endPos = new Vector2(isOn ? knobOnX : knobOffX, startPos.y);

        Color startColor = trackImage.color;
        Color endColor = isOn ? onColor : offColor;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);

            knob.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            trackImage.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        knob.anchoredPosition = endPos;
        trackImage.color = endColor;

        if (label != null)
            label.text = isOn ? onText : offText;
    }

    private void ApplyVisualsInstant(bool isOn)
    {
        knob.anchoredPosition = new Vector2(isOn ? knobOnX : knobOffX, knob.anchoredPosition.y);
        trackImage.color = isOn ? onColor : offColor;

        if (label != null)
            label.text = isOn ? onText : offText;
    }
}