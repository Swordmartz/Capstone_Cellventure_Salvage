using UnityEngine;

public class Trigger7 : MonoBehaviour
{
    public AIforDialogue MAI;          // Assign AIGuide

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            // Start your sequence
            StartCoroutine(MAI.DialogueSequenceIRB7());
        }
    }
}