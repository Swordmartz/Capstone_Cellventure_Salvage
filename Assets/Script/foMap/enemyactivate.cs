using UnityEngine;

public class ActivateOnDeactivate : MonoBehaviour
{
    public GameObject watchedObject;         // the object being watched
    public GameObject[] objectsToActivate;   // objects to activate when it deactivates
    public bool triggerOnce = true;

    private bool wasActive = false;
    private bool triggered = false;

    private void Start()
    {
        // Just record its current state — do NOT treat starting inactive as a trigger.
        if (watchedObject != null)
            wasActive = watchedObject.activeSelf;
    }

    private void Update()
    {
        if (watchedObject == null) return;
        if (triggered && triggerOnce) return;

        bool isActiveNow = watchedObject.activeSelf;

        // Only fires on a TRUE -> FALSE transition, never on the initial state.
        if (wasActive && !isActiveNow)
        {
            foreach (GameObject obj in objectsToActivate)
                obj?.SetActive(true);

            triggered = true;
        }

        wasActive = isActiveNow;
    }
}