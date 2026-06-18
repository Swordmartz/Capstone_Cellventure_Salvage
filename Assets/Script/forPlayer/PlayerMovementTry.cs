using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementTry : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 30f;

    [Header("Idle Tracking")]
    public float idleTime = 0f;
    public float idleThreshold = 2f;
    public float movementThreshold = 0.1f;

    [Header("Lost Tracking")]
    public float lostTime = 0f;
    public float lostThreshold = 2f;

    [Header("References")]
    public AIforDialogue dialogueSystem;
    public Joystick joystick;
    public GameObject missionBoard;
    public GameObject map;
    public AI_TestTD statsScript;
    public GameTimer gameTimer;

    Animator anim;
    private Vector2 lastMoveDirection = Vector2.down;


    private float idleCounter = 0f;
    private float lostCounter = 0f;

    private Rigidbody rb;
    private Vector2 movementInput;
    private Vector3 targetVelocity;

    public Vector3 lastInputDirection = Vector3.forward;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void Start()
    {
        // ✅ Initialize idle facing direction at start
        if (anim != null)
        {
            anim.SetFloat("LastMoveX", lastMoveDirection.x);
            anim.SetFloat("LastMoveY", lastMoveDirection.y);
        }
    }

    void Update()
    {
        if (gameTimer != null && !gameTimer.timerActive && gameTimer.GetCurrentTime() <= 0f)
            return;

        targetVelocity = new Vector3(movementInput.x, 0, movementInput.y) * moveSpeed;

        // ✅ Track last movement direction for idle facing
        if (movementInput.sqrMagnitude > 0.01f)
        {
            lastInputDirection = new Vector3(movementInput.x, 0, movementInput.y).normalized;
            lastMoveDirection = movementInput.normalized;

            // ✅ Update idle facing while moving
            UpdateIdleDirection(movementInput.normalized);
        }

        // ── Shared UI checks ──
        bool dialogueActive = dialogueSystem.dialoguePanel != null
                                 && dialogueSystem.dialoguePanel.activeSelf;
        bool missionBoardActive = missionBoard != null && missionBoard.activeSelf;
        bool mapActive = map != null && map.activeSelf;
        bool anyUIActive = dialogueActive || missionBoardActive || mapActive;

        // ── Movement & input checks ──
        bool isMoving = rb.linearVelocity.magnitude > movementThreshold;
        bool isTouching = Input.touchCount > 0;
        bool joystickActive = joystick != null
                              && joystick.Direction.magnitude > 0.1f;

        // ────────────────────────────────────────────
        // IDLE TIMER
        // ────────────────────────────────────────────
        bool isIdle = !anyUIActive
                   && !isMoving
                   && !isTouching
                   && !joystickActive;

        if (isIdle)
        {
            idleCounter += Time.deltaTime;
            if (idleCounter >= idleThreshold)
                idleTime += Time.deltaTime;
        }
        else
        {
            idleCounter = 0f;
        }

        // ────────────────────────────────────────────
        // LOST TIMER
        // ────────────────────────────────────────────
        bool actionButtonPressed = IsActionButtonPressed();

        bool isLost = !anyUIActive
                   && isMoving
                   && joystickActive
                   && !actionButtonPressed;

        if (isLost)
        {
            lostCounter += Time.deltaTime;
            if (lostCounter >= lostThreshold)
                lostTime += Time.deltaTime;
        }
        else
        {
            lostCounter = 0f;
        }

        SendStruggles();
    }

    // ✅ Only updates the idle facing direction
    private void UpdateIdleDirection(Vector2 direction)
    {
        if (anim == null) return;

        anim.SetFloat("LastMoveX", direction.x);
        anim.SetFloat("LastMoveY", direction.y);
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

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    public void SetMovementInput(Vector2 input)
    {
        movementInput = input;
    }

    public void ResetTimers()
    {
        idleTime = 0f;
        lostTime = 0f;
        idleCounter = 0f;
        lostCounter = 0f;
    }

    private bool IsActionButtonPressed()
    {
        if (Input.touchCount <= 1)
            return false;

        return Input.touchCount > 1;
    }

    public void SendStruggles()
    {
        if (statsScript == null) return;

        int struggle = Mathf.RoundToInt((idleTime * 0.6f) + (lostTime * 0.4f));
        statsScript.idleTime = Mathf.Clamp(struggle, 0, int.MaxValue);

        Debug.Log($"Struggle: {statsScript.idleTime}");
    }

   
}