using UnityEngine;

public class Trigger11 : MonoBehaviour
{
    public AIforDialogue aiScript; // assign your AI enemy in Inspector
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            // Start the coroutine by name (safer if method is inside aiScript)
            if (aiScript != null)
            {
                aiScript.StartCoroutine(aiScript.Dialogue2IWBCE());
            }
            else
            {
                Debug.LogWarning("AI script not assigned!");
            }
        }
    }
}