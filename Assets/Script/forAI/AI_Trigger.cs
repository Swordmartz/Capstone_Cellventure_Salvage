using UnityEngine;

public class AI_Trigger : MonoBehaviour
{
    public AIGuide guide;          // Assign AIGuide
    public float newMaxDistance = 5f; // Your custom distance

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            // 🎯 Set the max distance of the guide
            guide.maxDistance = newMaxDistance;

            // Start your sequence
            guide.StartCoroutine(guide.HandleTriggerSequence1());
        }
    }
}