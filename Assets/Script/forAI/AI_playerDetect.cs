using UnityEngine;

public class PlayerDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 5f; // Pathogen's threat radius

    void OnDrawGizmosSelected()
    {
        // Draw a green wire sphere around the pathogen to show detection range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}