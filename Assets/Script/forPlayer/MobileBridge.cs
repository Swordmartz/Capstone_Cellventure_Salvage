using UnityEngine;

[RequireComponent(typeof(PlayerMovementTry))]
public class MobileInputBridge : MonoBehaviour
{
    [SerializeField] private FloatingJoystick1 joystick;

    private PlayerMovementTry playerMovement;
    private static MobileInputBridge instance;

    public static bool inputLocked = false;

    void Awake()
    {
        instance = this;
        playerMovement = GetComponent<PlayerMovementTry>();

        if (joystick == null)
            Debug.LogWarning("[MobileInputBridge] FloatingJoystick reference is not assigned!");
    }

    void Update()
    {
        if (joystick == null) return;

        if (inputLocked || !joystick.gameObject.activeInHierarchy)
        {
            playerMovement.SetMovementInput(Vector2.zero);
            return;
        }

        // ✅ Clear input if joystick just became active
        if (joystick.Horizontal == 0 && joystick.Vertical == 0)
        {
            playerMovement.SetMovementInput(Vector2.zero);
            return;
        }

        playerMovement.SetMovementInput(new Vector2(joystick.Horizontal, joystick.Vertical));
    }

    public static void LockInput(bool locked)
    {
        inputLocked = locked;

        // Always zero out when locking
        if (locked && instance != null)
            instance.playerMovement.SetMovementInput(Vector2.zero);
    }

    // Call this after dialogue ends to force clear any stored input
    public static void ForceZero()
    {
        if (instance != null)
            instance.playerMovement.SetMovementInput(Vector2.zero);
    }
}