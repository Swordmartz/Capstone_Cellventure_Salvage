using UnityEngine;
using UnityEngine.UI;

public class ItemReceiver : MonoBehaviour
{
    [Header("UI")]
    public Button useButton; // Button to press
    public AI_TestTD halu;

    [Header("Item Requirement")]
    public bool requireItem = true;
    public Inventory playerInventory;
    public O2Item requiredItem;

    [Header("Consume Item")]
    public bool consumeItem = true;

    [Header("Reactivation")]
    public GameObject objectToReactivate; // ✅ drag the object you want to reactivate here

    private bool playerNearby = false;

    private void Start()
    {
        if (useButton != null)
        {
            useButton.gameObject.SetActive(false); // Start hidden
            useButton.onClick.AddListener(Execute);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            if (useButton != null)
                useButton.gameObject.SetActive(true); // Show button
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            if (useButton != null)
                useButton.gameObject.SetActive(false); // Hide button
        }
    }

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

            if (consumeItem)
            {
                playerInventory.ClearItem();
                Debug.Log("Item consumed.");
            }
        }

        // ✅ Increment the counter safely
        if (halu != null)
        {
            halu.itemsDelivered++;
            halu.UpdateCounterUI();
            Debug.Log("Items delivered: " + halu.itemsDelivered);
        }

        // ✅ Reactivate the assigned GameObject
        if (objectToReactivate != null)
        {
            objectToReactivate.SetActive(true);
            Debug.Log("Reactivated object: " + objectToReactivate.name);
        }

        if (useButton != null)
            useButton.gameObject.SetActive(false);

        // ✅ Deactivate this receiver object after successful item delivery
        gameObject.SetActive(false);
    }
}
