using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementTry : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 20f;

    private Rigidbody rb;
    private Vector2 movementInput;
    private Vector3 targetVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Calculate target velocity every frame (faster response)
        targetVelocity = new Vector3(movementInput.x, 0, movementInput.y) * moveSpeed;
    }

    void FixedUpdate()
    {
        // Get current velocity
        Vector3 velocity = rb.linearVelocity;

        // Calculate change needed
        Vector3 velocityChange = targetVelocity - new Vector3(velocity.x, 0, velocity.z);

        velocityChange = Vector3.ClampMagnitude(
            velocityChange,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(
            velocity.x + velocityChange.x,
            velocity.y,
            velocity.z + velocityChange.z
        );
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }
}