using UnityEngine;

public class PlayerTrigger9 : MonoBehaviour
{
    public AIforDialogue aiScript;
    public MissionSubmissionManager missionSubmission;
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            if (aiScript != null)
            {
                aiScript.StartCoroutine(aiScript.DialogueSequence0IWBCE());
            }
            else
            {
                Debug.LogWarning("AI script not assigned!");
            }

            // Tick mission index 0 on trigger
            if (missionSubmission != null)
            {
                missionSubmission.CompleteMissionByIndex(0);
            }
            else
            {
                Debug.LogWarning("MissionSubmissionManager not assigned!");
            }
        }
    }
}