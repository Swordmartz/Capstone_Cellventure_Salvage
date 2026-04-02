using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private Transform teleportTarget;

    // Reference to your GuideSystem script
    [SerializeField] private AI_TestTD AI_Test;
    [SerializeField] private AIforGuide guideSystem;

    // 🔥 NEW: Inventory check
    [Header("Item Requirement")]
    public bool requireItem = false;
    public Inventory playerInventory;      // Assign player inventory
    public O2Item requiredItem;            // The item needed

    [Header("Optional Object to Disable")]
    public GameObject optionalObjectToDisable; // Assign if you want to disable something

    public void Execute()
    {
        // 🧠 STEP 0: Check if item is required
        if (requireItem)
        {
            if (playerInventory == null)
            {
                Debug.LogWarning("Inventory not assigned.");
                return;
            }

            if (!playerInventory.HasItem || playerInventory.currentItem != requiredItem)
            {
                StartCoroutine(AI_Test.HandleTriggerSequence3());
                Debug.Log("Required item not found. Cannot execute.");
                return; // ❌ STOP everything
            }

            Debug.Log("Required item found. Proceeding...");
        }

        // Step 1: Disable guide system
        if (AI_Test != null)
        {
            guideSystem.guideEnabled = false;
            Debug.Log("Guide system deactivated.");
        }
        else
        {
            Debug.LogWarning("GuideSystem reference not assigned.");
        }

        // ✅ Step 1.5: Disable optional object if assigned
        if (optionalObjectToDisable != null)
        {
            optionalObjectToDisable.SetActive(false);
            Debug.Log(optionalObjectToDisable.name + " disabled.");
        }

        // Step 2: Teleport the player
        if (teleportTarget != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                Rigidbody rb = player.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.position = teleportTarget.position;
                }
                else
                {
                    player.transform.position = teleportTarget.position;
                }

                Debug.Log("Player teleported to " + teleportTarget.name);
            }
            else
            {
                Debug.LogWarning("Player not found for teleport.");
            }
        }
        else
        {
            Debug.LogWarning("Teleport target not assigned.");
        }
    }
}