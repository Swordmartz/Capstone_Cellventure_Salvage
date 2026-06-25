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
        playerMovement = GetComponent<PlayerMovementTry>();

        if (joystick == null)
            Debug.LogWarning("[MobileInputBridge] FloatingJoystick reference is not assigned!");
    }

    void OnEnable()
    {
        instance = this; // 👈 moved here so it updates when character switches
        playerMovement.SetMovementInput(Vector2.zero); // 👈 clear any leftover input
    }

    void OnDisable()
    {
        // 👈 clear input when this character gets disabled
        playerMovement.SetMovementInput(Vector2.zero);
    }

    void Update()
    {
        if (joystick == null) return;

        // Respect PlayerMovementTry's own attack lock (e.g. during rapid attack
        // bursts) in addition to this bridge's own inputLocked flag — otherwise
        // this script keeps feeding live joystick input straight through even
        // while an attack is supposed to be holding the player still.
        if (inputLocked || playerMovement.IsAttacking || !joystick.gameObject.activeInHierarchy)
        {
            playerMovement.SetMovementInput(Vector2.zero);
            return;
        }

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

        if (locked && instance != null)
            instance.playerMovement.SetMovementInput(Vector2.zero);
    }

    public static void ForceZero()
    {
        if (instance != null)
            instance.playerMovement.SetMovementInput(Vector2.zero);
    }
}