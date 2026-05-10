using UnityEngine;
using UnityEngine.UI;

public class ItemReceiverIncrement : MonoBehaviour
{
    [Header("UI")]
    public Button useButton;
    public AI_TestTD halu;

    [Header("Item Requirement")]
    public bool requireItem = true;
    public Inventory playerInventory;
    public O2Item requiredItem;
    public bool correctDelivery = true;

    [Header("Consume Item")]
    public bool consumeItem = true;

    [Header("Reactivation")]
    public GameObject objectToReactivate;

    [Header("Increment Settings")]
    public int deliveryIncrementAmount = 1;

    private bool playerNearby = false;

    private void Start()
    {
        if (useButton != null)
        {
            useButton.gameObject.SetActive(false);
            useButton.onClick.AddListener(Execute);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            if (useButton != null)
                useButton.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            if (useButton != null)
                useButton.gameObject.SetActive(false);
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
                Debug.Log("You don't have the required item!");
                return;
            }

            Debug.Log("Item received!");

            if (consumeItem)
            {
                playerInventory.ClearItem();
                Debug.Log("Item consumed.");
            }
        }

        if (requireItem && correctDelivery)
        {
            if (halu != null)
            {
                halu.itemsDelivered += deliveryIncrementAmount;
                halu.UpdateCounterUI();
                Debug.Log("Items delivered: " + halu.itemsDelivered);
            }
        }

        if (objectToReactivate != null)
        {
            objectToReactivate.SetActive(true);
            Debug.Log("Reactivated object: " + objectToReactivate.name);
        }

        if (useButton != null)
            useButton.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }
}