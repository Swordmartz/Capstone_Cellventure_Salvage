using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class SwitchToggleS : MonoBehaviour
{
    public Image FillImage;
    public Image HandleImage;
    public TextMeshProUGUI LabelText;

    private RectTransform handleRect;
    private bool isOn = false;
    private float targetFillAmout;

    public Color onColor = new Color(37f / 255f, 37f / 255f, 37f / 255);
    public Color offColor = Color.white;

    private void Start()
    {
        handleRect = HandleImage.GetComponent<RectTransform>();

        handleRect.pivot = new Vector2(0f, 0.5f);

        targetFillAmout = isOn ? 1f : 0f;

        UpdateHandlePivot();
        UpdateFillAmount();
        UpdateStateText();
    }

    public void ToggleSwitch()
    {
        isOn = !isOn;

        targetFillAmout = isOn ? 1f : 0f;

        UpdateHandlePivot();
        UpdateStateText();
    }

    private void Update()
    {
        float fillspeed = 5f;

        FillImage.fillAmount = Mathf.Lerp(FillImage.fillAmount, targetFillAmout, Time.deltaTime * fillspeed);

        float PosX = isOn ? FillImage.rectTransform.rect.width : 0f;
        handleRect.anchoredPosition = new Vector2(Mathf.Lerp(handleRect.anchoredPosition.x, PosX, Time.deltaTime * 8f), handleRect.anchoredPosition.y);
    }

    private void UpdateFillAmount()
    {
        FillImage.fillAmount = targetFillAmout;

        handleRect.anchoredPosition = new Vector2(targetFillAmout * FillImage.rectTransform.rect.width, handleRect.anchoredPosition.y);

    }

    private void UpdateStateText()
    {
        LabelText.text = isOn ? "ON" : "OFF";
        LabelText.color = isOn ? onColor : offColor;
    }

    private void UpdateHandlePivot()
    {
        if (isOn)
            handleRect.pivot = new Vector2(1f, 0.5f);
        else
            handleRect.pivot = new Vector2(0f, 0.5f);
    }
}