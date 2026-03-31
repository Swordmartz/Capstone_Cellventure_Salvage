using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using UnityEngine.UI;

public class FloatingJoystickET : MonoBehaviour
{
    [Header("Joystick UI")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public float handleLimit = 100f;

    private Vector2 inputVector = Vector2.zero;
    private Finger activeFinger;

    public Vector2 InputVector => inputVector;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        Touch.onFingerDown += HandleFingerDown;
        Touch.onFingerUp += HandleFingerUp;
        Touch.onFingerMove += HandleFingerMove;
    }

    void OnDisable()
    {
        Touch.onFingerDown -= HandleFingerDown;
        Touch.onFingerUp -= HandleFingerUp;
        Touch.onFingerMove -= HandleFingerMove;
        EnhancedTouchSupport.Disable();
    }

    private void HandleFingerDown(Finger finger)
    {
        if (activeFinger != null) return; // only one finger controls joystick

        activeFinger = finger;
        joystickBackground.position = finger.screenPosition;
        joystickBackground.gameObject.SetActive(true);
        joystickHandle.localPosition = Vector2.zero;
    }

    private void HandleFingerMove(Finger finger)
    {
        if (finger != activeFinger) return;

        Vector2 direction = finger.screenPosition - (Vector2)joystickBackground.position;
        Vector2 clamped = Vector2.ClampMagnitude(direction, handleLimit);

        joystickHandle.localPosition = clamped;
        inputVector = clamped / handleLimit;
    }

    private void HandleFingerUp(Finger finger)
    {
        if (finger != activeFinger) return;

        activeFinger = null;
        joystickBackground.gameObject.SetActive(false);
        joystickHandle.localPosition = Vector2.zero;
        inputVector = Vector2.zero;
    }
}
