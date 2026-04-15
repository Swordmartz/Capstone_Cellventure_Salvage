using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick3 : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick Settings")]
    [SerializeField] private float handleRange = 80f;
    [SerializeField] private float deadZone = 0.1f;

    [Header("References")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    public Vector2 Direction { get; private set; }

    private RectTransform rectTransform;
    private Canvas canvas;
    private Camera canvasCamera;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        background.gameObject.SetActive(false);
    }

    // Force reset when joystick GameObject is disabled
    void OnDisable()
    {
        ResetJoystick();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        background.gameObject.SetActive(true);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            canvasCamera,
            out localPoint);

        background.anchoredPosition = localPoint;
        handle.anchoredPosition = Vector2.zero;

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            canvasCamera,
            out localPoint);

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);
        handle.anchoredPosition = clamped;

        Vector2 normalized = clamped / handleRange;
        Direction = normalized.magnitude < deadZone ? Vector2.zero : normalized;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetJoystick();
    }

    private void ResetJoystick()
    {
        Direction = Vector2.zero;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        if (background != null)
            background.gameObject.SetActive(false);
    }
}