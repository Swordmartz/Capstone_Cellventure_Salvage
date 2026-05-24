using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SlidingRadioButtons : MonoBehaviour
{
    [Header("Buttons")]
    public Button leftButton;
    public Button rightButton;

    [Header("Button RectTransforms")]
    public RectTransform leftButtonRect;
    public RectTransform rightButtonRect;

    [Header("Button Positions")]
    public RectTransform leftButtonLeftPosition;
    public RectTransform leftButtonRightPosition;
    public RectTransform rightButtonLeftPosition;
    public RectTransform rightButtonRightPosition;

    [Header("Objects To Toggle")]
    public GameObject leftObject;
    public GameObject rightObject;

    [Header("Animation")]
    public float slideDuration = 0.25f;

    private Coroutine slideCoroutine;
    private bool isLeftActive = true;

    private void Start()
    {
        if (leftButton != null)
            leftButton.onClick.AddListener(SelectLeft);

        if (rightButton != null)
            rightButton.onClick.AddListener(SelectRight);

        SelectLeftInstant();
    }

    public void SelectLeft()
    {
        if (isLeftActive)
            return;

        isLeftActive = true;

        if (leftObject != null)
            leftObject.SetActive(true);

        if (rightObject != null)
            rightObject.SetActive(false);

        StartSlide(
            leftButtonLeftPosition.anchoredPosition,
            rightButtonRightPosition.anchoredPosition
        );
    }

    public void SelectRight()
    {
        if (!isLeftActive)
            return;

        isLeftActive = false;

        if (leftObject != null)
            leftObject.SetActive(false);

        if (rightObject != null)
            rightObject.SetActive(true);

        StartSlide(
            leftButtonRightPosition.anchoredPosition,
            rightButtonLeftPosition.anchoredPosition
        );
    }

    private void StartSlide(Vector2 leftTarget, Vector2 rightTarget)
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(SlideButtons(leftTarget, rightTarget));
    }

    private IEnumerator SlideButtons(Vector2 leftTarget, Vector2 rightTarget)
    {
        Vector2 leftStart = leftButtonRect.anchoredPosition;
        Vector2 rightStart = rightButtonRect.anchoredPosition;

        float timer = 0f;

        while (timer < slideDuration)
        {
            timer += Time.deltaTime;
            float t = timer / slideDuration;

            // Smooth movement
            t = t * t * (3f - 2f * t);

            if (leftButtonRect != null)
                leftButtonRect.anchoredPosition = Vector2.Lerp(leftStart, leftTarget, t);

            if (rightButtonRect != null)
                rightButtonRect.anchoredPosition = Vector2.Lerp(rightStart, rightTarget, t);

            yield return null;
        }

        if (leftButtonRect != null)
            leftButtonRect.anchoredPosition = leftTarget;

        if (rightButtonRect != null)
            rightButtonRect.anchoredPosition = rightTarget;
    }

    private void SelectLeftInstant()
    {
        isLeftActive = true;

        if (leftObject != null)
            leftObject.SetActive(true);

        if (rightObject != null)
            rightObject.SetActive(false);

        if (leftButtonRect != null && leftButtonLeftPosition != null)
            leftButtonRect.anchoredPosition = leftButtonLeftPosition.anchoredPosition;

        if (rightButtonRect != null && rightButtonRightPosition != null)
            rightButtonRect.anchoredPosition = rightButtonRightPosition.anchoredPosition;
    }
}