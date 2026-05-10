using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class DraggableObject : MonoBehaviour
{
    [Header("Movement Constraints")]
    [SerializeField] private bool lockZAxis = true;
    [SerializeField] private float fixedZ = 0f;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;
    [SerializeField] private float minY = -10f;
    [SerializeField] private float maxY = 10f;

    [Header("Physics")]
    [SerializeField] private float dragPhysicsDrag = 8f;
    [SerializeField] private float normalPhysicsDrag = 1f;
    [SerializeField] private bool useGravity = false;
    [SerializeField] private bool freezeRotation = true;

    [Header("Visual Feedback")]
    [SerializeField] private bool scaleOnDrag = true;
    [SerializeField] private float dragScale = 1.1f;
    [SerializeField] private float scaleSpeed = 10f;

    [Header("State")]
    [SerializeField] private bool isDraggable = true;

    private Rigidbody rb;
    private bool isDragging;
    private Vector3 targetPosition;
    private Vector3 originalScale;
    private Vector3 desiredScale;
    private float currentSmoothness = 0.1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
        desiredScale = originalScale;
        targetPosition = transform.position;

        // Auto-sync fixedZ to actual object Z so inspector value doesn't matter
        fixedZ = transform.position.z;

        rb.linearDamping = normalPhysicsDrag;
        rb.useGravity = useGravity;
        rb.isKinematic = false;

        if (freezeRotation)
            rb.constraints = RigidbodyConstraints.FreezeRotation;

        Debug.Log($"[DraggableObject] {name} initialized | pos:{transform.position} | fixedZ:{fixedZ} | isKinematic:{rb.isKinematic} | constraints:{rb.constraints}");
    }

    private void Update()
    {
        if (scaleOnDrag)
            transform.localScale = Vector3.Lerp(transform.localScale, desiredScale,
                                                 Time.deltaTime * scaleSpeed);
    }

    private void FixedUpdate()
    {
        if (isDragging)
            MoveToTarget();
    }

    public bool CanDrag() => isDraggable && !isDragging;

    public void OnDragStart()
    {
        isDragging = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.linearDamping = dragPhysicsDrag;

        if (scaleOnDrag)
            desiredScale = originalScale * dragScale;

        Debug.Log($"[DraggableObject] {name} OnDragStart | pos:{transform.position} | isKinematic:{rb.isKinematic}");
    }

    public void UpdateDragPosition(Vector3 newTargetPosition, float smoothness)
    {
        currentSmoothness = smoothness;

        newTargetPosition.x = Mathf.Clamp(newTargetPosition.x, minX, maxX);
        newTargetPosition.y = Mathf.Clamp(newTargetPosition.y, minY, maxY);

        if (lockZAxis)
            newTargetPosition.z = fixedZ;

        targetPosition = newTargetPosition;
    }

    public void OnDragEnd()
    {
        isDragging = false;
        rb.linearDamping = normalPhysicsDrag;

        if (scaleOnDrag)
            desiredScale = originalScale;

        Debug.Log($"[DraggableObject] {name} OnDragEnd");
    }

    public void EnableDragging() => isDraggable = true;

    public void DisableDragging()
    {
        isDraggable = false;
        if (isDragging) OnDragEnd();
    }

    public bool IsDragging() => isDragging;

    private void MoveToTarget()
    {
        float alpha = Mathf.Clamp01(currentSmoothness * 60f * Time.fixedDeltaTime);
        Vector3 next = Vector3.Lerp(rb.position, targetPosition, alpha);

        Debug.Log($"[DraggableObject] {name} MoveToTarget | rb.pos:{rb.position} | target:{targetPosition} | alpha:{alpha} | next:{next}");

        rb.MovePosition(next);

        if (freezeRotation)
            rb.angularVelocity = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isDraggable ? Color.green : Color.red;
        float z = lockZAxis ? fixedZ : transform.position.z;
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, z);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.1f);
        Gizmos.DrawWireCube(center, size);
    }
}