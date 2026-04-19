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
    public GameObject miniScreen;
    public Button brainButton;
    public Button muscleButton;
    public Button heartButton;
    public Transform player;
    public Transform brainTarget;
    public Transform muscleTarget;
    public Transform heartTarget;

    [Header("Floor Visibility")]
    public bool enableFloorVisibility = false;
    public string[] layersToHide;          // manually type layer names to hide on teleport

    private int originalCullingMask;

    private void Start()
    {
        // Save original culling mask
        originalCullingMask = Camera.main.cullingMask;

        if (miniScreen != null)
            miniScreen.SetActive(false);

        if (brainButton != null) brainButton.onClick.AddListener(() => TeleportPlayer(brainTarget));
        if (muscleButton != null) muscleButton.onClick.AddListener(() => TeleportPlayer(muscleTarget));
        if (heartButton != null) heartButton.onClick.AddListener(() => TeleportPlayer(heartTarget));
    }

    public void Execute()
    {
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
                return;
            }
        }

        if (AI_Test != null && guideSystem != null)
            guideSystem.guideEnabled = false;

        if (optionalObjectToDisable != null)
            optionalObjectToDisable.SetActive(false);

        // ✅ Hide layers BEFORE teleporting
        if (enableFloorVisibility)
            ApplyLayerVisibility();

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
            }
        }

        if (miniScreen != null)
            miniScreen.SetActive(true);
    }

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
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = destination.position;
            }
            else
            {
                playerObj.transform.position = destination.position;
            }
        }

        if (enableFloorVisibility)
            ApplyLayerVisibility();

        if (miniScreen != null)
            miniScreen.SetActive(false);
    }

    private void ApplyLayerVisibility()
    {
        int newMask = originalCullingMask;

        foreach (string layerName in layersToHide)
        {
            int layer = LayerMask.NameToLayer(layerName);
            Debug.Log($"Hiding layer: '{layerName}' = index {layer}");
            if (layer == -1)
            {
                Debug.LogWarning($"Layer '{layerName}' not found!");
                continue;
            }
            newMask &= ~(1 << layer);
        }

        Debug.Log($"Old mask: {originalCullingMask} | New mask: {newMask}");
        Camera.main.cullingMask = newMask;
    }

    public void RestoreCullingMask()
    {
        Camera.main.cullingMask = originalCullingMask;
    }
}