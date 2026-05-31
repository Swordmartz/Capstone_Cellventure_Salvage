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
    public GameObject[] objectToReactivate;

    [Header("Increment Settings")]
    public int deliveryIncrementAmount = 1;

    [Header("Mission Submission")]
    public bool completeMissionOnExecute = false;
    [Tooltip("Which mission index to complete (0 = first, 1 = second, etc.)")]
    public int missionIndex = 0;
    public MissionSubmissionManager missionManager;

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

            if (!playerInventory.HasItem)
            {
                Debug.Log("No item in inventory!");
                return;
            }

            if (playerInventory.currentItem != requiredItem)
            {
                Debug.Log("Wrong item delivered: " + playerInventory.currentItem.itemName);

                if (halu != null)
                {
                    halu.FailedDelivery += deliveryIncrementAmount;
                    Debug.Log("Failed deliveries: " + halu.FailedDelivery);
                }

                if (consumeItem)
                {
                    playerInventory.ClearItem();
                    Debug.Log("Wrong item consumed.");
                }

                return;
            }

            Debug.Log("Correct item received: " + playerInventory.currentItem.itemName);

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
                halu.RegisterDelivery(deliveryIncrementAmount);
            }
        }

        if (objectToReactivate != null && objectToReactivate.Length > 0)
        {
            foreach (GameObject obj in objectToReactivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log("Reactivated: " + obj.name);
                }
            }
        }

        // Complete mission at specified index
        if (completeMissionOnExecute)
        {
            if (missionManager != null)
            {
                missionManager.CompleteMissionByIndex(missionIndex);
                Debug.Log("[ItemReceiverIncrement] Mission " + missionIndex + " completed.");
            }
            else
            {
                Debug.LogWarning("[ItemReceiverIncrement] completeMissionOnExecute is true but missionManager is not assigned.");
            }
        }

        if (useButton != null)
            useButton.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }
}