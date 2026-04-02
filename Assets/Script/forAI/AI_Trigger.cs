using UnityEngine;

public class AI_Trigger : MonoBehaviour
{
    public AI_TestTD MAI;          // Assign AIGuide
    public AIforGuide AIG;
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
            MAI.StartCoroutine(MAI.HandleTriggerSequence1());
        }
    }
}