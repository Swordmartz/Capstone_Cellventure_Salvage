using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementTry : MonoBehaviour
{

    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 30f; // braking force

    private Rigidbody rb;
    private Vector2 movementInput;
    private Vector3 targetVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // prevent unwanted physics rotation
    }

    void Update()
    {
        // Desired velocity based on input
        targetVelocity = new Vector3(movementInput.x, 0, movementInput.y) * moveSpeed;
    }

    void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity; //  use linearVelocity
        Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);

        if (movementInput.sqrMagnitude > 0.01f)
        {
            // Accelerate toward target velocity
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
            // Decelerate smoothly when no input
            horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero, deceleration * Time.fixedDeltaTime);

            rb.linearVelocity = new Vector3(
                horizontalVel.x,
                velocity.y,
                horizontalVel.z
            );
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }
}
