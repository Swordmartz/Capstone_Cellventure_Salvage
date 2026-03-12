using UnityEngine;

public class SphereInteractUI : MonoBehaviour
{
    [Header("Detection Settings")]
    public string playerTag = "Player";       // Tag to identify the player
    public GameObject interactButtonUI;       // Assign your UI button here

    [Header("Item Reference")]
    public Item itemScript;                   // Drag your Item script here

    private bool playerInRange = false;

    void Start()
    {
        // Hide the button at start
        if (interactButtonUI != null)
            interactButtonUI.SetActive(false);

        // Auto‑grab Item script if it's on the same GameObject
        if (itemScript == null)
            itemScript = GetComponent<Item>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            if (interactButtonUI != null)
                interactButtonUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            if (interactButtonUI != null)
                interactButtonUI.SetActive(false);
        }
    }

    // Called by the UI button
    public void OnInteract()
    {
        if (playerInRange && itemScript != null)
        {
            // Call the item’s custom logic
            itemScript.Execute();

            Debug.Log("Sphere interacted, Item script executed!");
        }
    }
}
