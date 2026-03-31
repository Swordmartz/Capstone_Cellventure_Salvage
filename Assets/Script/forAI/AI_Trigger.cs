using UnityEngine;

public class AI_Trigger : MonoBehaviour
{
    public AIGuide guide; // Assign your manager script in inspector
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        // Only trigger for the player
        if (other.CompareTag("Player"))
        {
            triggered = true;
            guide.StartCoroutine(guide.HandleTriggerSequence());
        }
    }
}