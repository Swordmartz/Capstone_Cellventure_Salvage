using UnityEngine;

public class SuperBarFullTrigger2 : MonoBehaviour
{
    public SliderTimer superBar;   // assign your super bar in Inspector
    public AIforDialogue aiScript; // assign your AI script in Inspector
    private bool triggered = false;

    private void Update()
    {
        if (triggered) return;

        if (superBar != null && superBar.IsFull)
        {
            triggered = true;

            if (aiScript != null)
            {
                aiScript.StartCoroutine(aiScript.Dialogue6IWBCE());
            }
            else
            {
                Debug.LogWarning("AI script not assigned!");
            }
        }
    }
}