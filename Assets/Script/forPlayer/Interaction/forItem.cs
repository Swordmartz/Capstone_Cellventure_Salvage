using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private Transform teleportTarget;

    // Reference to your GuideSystem script
    [SerializeField] private AIGuide guideSystem;

    public void Execute()
    {
        // Step 1: Disable guide system by setting its bool to false
        if (guideSystem != null)
        {
            guideSystem.guideEnabled = false; // set the guide flag to false
            Debug.Log("Guide system deactivated.");
        }
        else
        {
            Debug.LogWarning("GuideSystem reference not assigned.");
        }

        // Step 2: Teleport the player
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

                    // Teleport player
                    rb.position = teleportTarget.position;
                }
                else
                {
                    // Fallback if no Rigidbody
                    player.transform.position = teleportTarget.position;
                }

                Debug.Log("Player teleported to " + teleportTarget.name);
            }
            else
            {
                Debug.LogWarning("Player not found for teleport.");
            }
        }
        else
        {
            Debug.LogWarning("Teleport target not assigned.");
        }
    }
}