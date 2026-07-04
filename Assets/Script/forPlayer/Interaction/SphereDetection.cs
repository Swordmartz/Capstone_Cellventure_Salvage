using UnityEngine;
using System.Collections.Generic;

public class SphereInteractUI : MonoBehaviour
{
    [Header("Detection Settings")]
    public string playerTag = "Player";              // Tag to identify the player
    public List<GameObject> interactButtonUIs;       // Assign your UI buttons here

    [Header("Item Reference")]
    public Item itemScript;                          // Drag your Item script here

    private bool playerInRange = false;

    void Start()
    {
        // Hide all buttons at start
        SetButtonsActive(false);

        // Auto‑grab Item script if it's on the same GameObject
        if (itemScript == null)
            itemScript = GetComponent<Item>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            SetButtonsActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            SetButtonsActive(false);
        }
    }

    private void SetButtonsActive(bool state)
    {
        if (interactButtonUIs == null) return;

        foreach (GameObject button in interactButtonUIs)
        {
            if (button != null)
                button.SetActive(state);
        }
    }

    // Called by the UI button
    public void OnInteract()
    {
        if (playerInRange && itemScript != null)
        {
            // Call the item's custom logic
            itemScript.Execute();

            Debug.Log("Sphere interacted, Item script executed!");
        }
    }
}