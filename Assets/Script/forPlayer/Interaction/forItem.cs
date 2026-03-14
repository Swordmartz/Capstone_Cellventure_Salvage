using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private Transform teleportTarget;

    public void Execute()
    {
        // Example behavior: hide the item when picked up
        if (teleportTarget != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                Rigidbody rb = player.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    // Stop movement
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    // Teleport safely
                    rb.position = teleportTarget.position;
                }
                else
                {
                    // Fallback if no Rigidbody
                    player.transform.position = teleportTarget.position;
                }

                Debug.Log("Player teleported to " + teleportTarget.name);
            }
        }
    }
}
