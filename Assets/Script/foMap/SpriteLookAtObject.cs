using UnityEngine;

public class BillboardToTarget : MonoBehaviour
{
    [Header("Target to face")]
    public Transform target;

    [Header("Options")]
    public bool lockYAxis = false;   // only rotate around Y (keeps it upright, like a name tag)
    public bool flip180 = false;     // flip if the object appears backwards

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 direction = target.position - transform.position;

        if (lockYAxis)
            direction.y = 0f; // ignore height difference, only rotate horizontally

        if (direction.sqrMagnitude < 0.0001f) return; // avoid errors if too close/overlapping

        Quaternion lookRotation = Quaternion.LookRotation(direction);

        if (flip180)
            lookRotation *= Quaternion.Euler(0f, 180f, 0f);

        transform.rotation = lookRotation;
    }
}