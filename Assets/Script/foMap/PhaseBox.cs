using UnityEngine;

public class PlayerTriggerZone : MonoBehaviour
{
    public Rigidbody targetBody;

    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == targetBody)
        {
            other.isTrigger = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody == targetBody)
        {
            other.isTrigger = false;
        }
    }
}