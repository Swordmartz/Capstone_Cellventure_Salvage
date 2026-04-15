using UnityEngine;

public class AI_Trigger : MonoBehaviour
{
    public AIforDialogue MAI;          // Assign AIGuide
    public AIforGuide AIG;
            // Assign your AI_TestTD script
    public float newMaxDistance = 5f; // Your custom distance

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            // 🎯 Set the max distance of the guide
            AIG.maxDistance = newMaxDistance;

            // Start your sequence
            StartCoroutine(MAI.DialogueSequence1IRBC());
        }
    }
}