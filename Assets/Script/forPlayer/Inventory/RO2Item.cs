using UnityEngine;
using UnityEngine.UI;

public class PickupButton : MonoBehaviour
{
    public Inventory playerInventory;
    public O2Item itemToPickup;
    public Button inventoryButton;
    public Image inventoryImage;
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
        if (itemToPickup == null)
        {
            Debug.LogError("itemToPickup not assigned on: " + gameObject.name);
            return;
        }

        // If no inventory assigned, silently skip — player can't pick up
        if (playerInventory == null)
            return;

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

    private void PasserbyPickup(PasserbyItemPickup passerby)
    {
        if (passerby == null) return;
        if (passerby.hasPickedUp) return;
        if (passerby.requiredItem != itemToPickup) return;

        passerby.ReceiveItem(itemToPickup, gameObject);
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            PickupItem();

        if (other.CompareTag("Passerby"))
            PasserbyPickup(other.GetComponent<PasserbyItemPickup>());
    }
}