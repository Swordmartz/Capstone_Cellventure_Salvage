using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Item : MonoBehaviour
{
    [SerializeField] private Transform teleportTarget;

    [Header("References")]
    [SerializeField] private AIforDialogue AI_Test;
    [SerializeField] private AIforGuide guideSystem;

    [Header("Item Requirement")]
    public bool requireItem = false;
    public Inventory playerInventory;
    public O2Item requiredItem;

    [Header("Optional Object to Disable")]
    public GameObject optionalObjectToDisable;

    [Header("Mini‑Screen Teleport")]
    public GameObject miniScreen;          // ✅ assign your mini‑screen panel
    public Button brainButton;             // ✅ teleport to brain
    public Button muscleButton;            // ✅ teleport to muscle
    public Button heartButton;             // ✅ teleport to heart
    public Transform player;               // ✅ player transform
    public Transform brainTarget;          // ✅ destination for brain
    public Transform muscleTarget;         // ✅ destination for muscle
    public Transform heartTarget;          // ✅ destination for heart

    private void Start()
    {
        if (miniScreen != null)
            miniScreen.SetActive(false);

        // Hook up teleport buttons
        if (brainButton != null) brainButton.onClick.AddListener(() => TeleportPlayer(brainTarget));
        if (muscleButton != null) muscleButton.onClick.AddListener(() => TeleportPlayer(muscleTarget));
        if (heartButton != null) heartButton.onClick.AddListener(() => TeleportPlayer(heartTarget));
    }

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
                StartCoroutine(AI_Test.DialogueSequence3IRBC());
                Debug.Log("Required item not found. Cannot execute.");
                return;
            }

            Debug.Log("Required item found. Proceeding...");
        }

        // Step 1: Disable guide system
        if (AI_Test != null && guideSystem != null)
        {
            guideSystem.guideEnabled = false;
            Debug.Log("Guide system deactivated.");
        }

        // Step 1.5: Disable optional object if assigned
        if (optionalObjectToDisable != null)
        {
            optionalObjectToDisable.SetActive(false);
            Debug.Log(optionalObjectToDisable.name + " disabled.");
        }

        // Step 2: Teleport the player (default target)
        if (teleportTarget != null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                Rigidbody rb = playerObj.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.position = teleportTarget.position;
                }
                else
                {
                    playerObj.transform.position = teleportTarget.position;
                }

                Debug.Log("Player teleported to " + teleportTarget.name);
            }
            else
            {
                Debug.LogWarning("Player not found for teleport.");
            }
        }

        // ✅ Step 3: Activate mini‑screen for teleport choices
        if (miniScreen != null)
        {
            miniScreen.SetActive(true);
            Debug.Log("Mini‑screen activated!");
        }
    }

    // ✅ Teleport player to chosen destination and hide mini‑screen
    private void TeleportPlayer(Transform destination)
    {
        if (destination == null)
        {
            Debug.LogWarning("Teleport destination not assigned.");
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            Rigidbody rb = playerObj.GetComponent<Rigidbody>();

            if (rb != null)
            {
                // Reset physics before teleport
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = destination.position;
            }
            else
            {
                // Fallback if no Rigidbody
                playerObj.transform.position = destination.position;
            }

            Debug.Log("Player teleported safely to " + destination.name);
        }
        else
        {
            Debug.LogWarning("Player not found for teleport.");
        }

        if (miniScreen != null)
            miniScreen.SetActive(false);
    }

}
