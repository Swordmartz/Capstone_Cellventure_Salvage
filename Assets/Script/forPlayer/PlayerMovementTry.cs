using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementTry : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 30f;

    private Rigidbody rb;
    private Vector2 movementInput;
    private Vector3 targetVelocity;

    // Store last joystick direction for attacks
    public Vector3 lastInputDirection = Vector3.forward;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        targetVelocity = new Vector3(movementInput.x, 0, movementInput.y) * moveSpeed;

        // Update last direction if joystick moved
        if (movementInput.sqrMagnitude > 0.01f)
        {
            lastInputDirection = new Vector3(movementInput.x, 0, movementInput.y).normalized;
        }
    }

    void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);

        if (movementInput.sqrMagnitude > 0.01f)
        {
            Vector3 velocityChange = targetVelocity - horizontalVel;
            velocityChange = Vector3.ClampMagnitude(velocityChange, acceleration * Time.fixedDeltaTime);

            rb.linearVelocity = new Vector3(
                horizontalVel.x + velocityChange.x,
                velocity.y,
                horizontalVel.z + velocityChange.z
            );
        }
        else
        {
            horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero, deceleration * Time.fixedDeltaTime);

            rb.linearVelocity = new Vector3(
                horizontalVel.x,
                velocity.y,
                horizontalVel.z
            );
        }
    }

    // Called by Input System (keyboard / gamepad via Player Input component)
    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    // ── Mobile bridge entry point ────────────────────────────────────────────
    /// <summary>
    /// Called by MobileInputBridge every frame with the floating joystick's
    /// normalized direction. Works alongside OnMove — whichever runs last wins,
    /// so on mobile you simply won't have a keyboard driving OnMove.
    /// </summary>
    public void SetMovementInput(Vector2 input)
    {
        movementInput = input;
    }
}