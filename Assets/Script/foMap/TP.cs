using UnityEngine;

public class TeleportToTarget : MonoBehaviour
{
    public Transform target;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Hook this up to the Button's OnClick() in the Inspector
    public void Teleport()
    {
        if (target == null)
        {
            Debug.LogWarning("Teleport target not set on " + gameObject.name);
            return;
        }

        // Stop all movement first so no leftover momentum carries through the teleport
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.position = target.position;
    }
}