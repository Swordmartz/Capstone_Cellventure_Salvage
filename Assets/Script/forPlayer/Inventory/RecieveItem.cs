using UnityEngine;
using UnityEngine.UI;

public class ItemReceiver : MonoBehaviour
{
    [Header("UI")]
    public Button useButton; // Button to press

    [Header("Item Requirement")]
    public bool requireItem = true;
    public Inventory playerInventory;
    public O2Item requiredItem;

    [Header("Consume Item")]
    public bool consumeItem = true;

    private bool playerNearby = false;

    private void Start()
    {
        if (useButton != null)
        {
            useButton.gameObject.SetActive(false); // Start hidden
            useButton.onClick.AddListener(Execute);
        }
    }

    // 🟢 When player enters range
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;

            if (useButton != null)
                useButton.gameObject.SetActive(true); // Show button
        }
    }

    // 🔴 When player leaves range
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;

            if (useButton != null)
                useButton.gameObject.SetActive(false); // Hide button
        }
    }

    // 🎯 When button is pressed
    public void Execute()
    {
        if (!playerNearby) return;

        if (requireItem)
        {
            if (playerInventory == null || requiredItem == null)
            {
                Debug.LogWarning("Missing references.");
                return;
            }

            if (!playerInventory.HasItem || playerInventory.currentItem != requiredItem)
            {
                Debug.Log("You don’t have the required item!");
                return;
            }

            Debug.Log("Item received!");

            // Consume item if needed
            if (consumeItem)
            {
                playerInventory.ClearItem();
                Debug.Log("Item consumed.");
            }
        }

        // Hide button after use
        if (useButton != null)
            useButton.gameObject.SetActive(false);
    }
}