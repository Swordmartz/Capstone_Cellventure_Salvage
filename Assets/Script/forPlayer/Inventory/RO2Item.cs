using UnityEngine;
using UnityEngine.UI;

public class PickupButton : MonoBehaviour
{
    public Inventory playerInventory;    // Player's inventory
    public O2Item itemToPickup;          // The ScriptableObject data
    public Button inventoryButton;       // Button to show item description
    public Button pickupButton;          // Button to pick up this item
    public Image inventoryImage;         // UI Image where the icon will appear
    [SerializeField] private AIGuide guideSystem;


    private void Start()
    {
        pickupButton.gameObject.SetActive(false); // Start inactive
        pickupButton.onClick.AddListener(PickupItem);

        if (inventoryImage != null)
        {
            inventoryImage.enabled = false; // Start hidden
        }
    }

    void PickupItem()
    {
        if (guideSystem != null)
        {
            guideSystem.guideEnabled = false; // disable guide first
            Debug.Log("Guide system deactivated.");

            // 🔥 START the trigger sequence
            StartCoroutine(guideSystem.HandleTriggerSequence2());
        }
        else
        {
            Debug.LogWarning("GuideSystem reference not assigned.");
        }

        if (playerInventory.HasItem)
        {
            Debug.Log("Inventory full! You already have an item.");
            return;
        }

        // Add item to inventory
        playerInventory.AddItem(itemToPickup);

        // Enable the inventory description button
        inventoryButton.gameObject.SetActive(false);

        // Show the icon automatically in the UI
        if (inventoryImage != null && itemToPickup.icon != null)
        {
            inventoryImage.sprite = itemToPickup.icon;
            inventoryImage.enabled = true;
        }

        // Hide the pickup button
        pickupButton.gameObject.SetActive(false);

        // Disable the scene object so it disappears
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            pickupButton.gameObject.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            pickupButton.gameObject.SetActive(false);
    }
}
