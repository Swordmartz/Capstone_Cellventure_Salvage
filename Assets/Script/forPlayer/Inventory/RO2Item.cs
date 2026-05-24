using UnityEngine;
using UnityEngine.UI;

public class PickupButton : MonoBehaviour
{
    public Inventory playerInventory;    // Player's inventory
    public O2Item itemToPickup;          // Any item that extends O2Item
    public Button inventoryButton;       // Button to show item description
    public Image inventoryImage;         // UI Image where the icon will appear
    [SerializeField] private AIforGuide guideSystem;
    [SerializeField] private AIforDialogue AIM;

    private bool dialogueTriggered = false;

    private void Start()
    {
        if (inventoryImage != null)
            inventoryImage.enabled = false;
    }

    void PickupItem()
    {
        if (playerInventory == null)
        {
            Debug.LogError("playerInventory not assigned on: " + gameObject.name);
            return;
        }

        if (itemToPickup == null)
        {
            Debug.LogError("itemToPickup not assigned on: " + gameObject.name);
            return;
        }

        if (playerInventory.HasItem)
        {
            Debug.Log("Inventory full!");
            return;
        }

        if (guideSystem != null)
        {
            guideSystem.guideEnabled = false;
            if (!dialogueTriggered && AIM != null)
            {
                if (itemToPickup.itemName == "Oxxygen")
                    StartCoroutine(AIM.DialogueSequence2IRBC());
                dialogueTriggered = true;
            }
        }
        
        else
        {
            Debug.LogWarning("GuideSystem not assigned on: " + gameObject.name);
        }

        playerInventory.AddItem(itemToPickup);

        if (inventoryButton != null)
            inventoryButton.gameObject.SetActive(false);

        if (inventoryImage != null && itemToPickup.icon != null)
        {
            inventoryImage.sprite = itemToPickup.icon;
            inventoryImage.enabled = true;
        }

        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            PickupItem();
    }
}