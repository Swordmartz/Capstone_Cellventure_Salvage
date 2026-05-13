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
    public string[] layersToHide;

    [Header("Secondary Camera Teleport")]
    public bool teleportSecondaryCamera = false;
    public Camera secondaryCamera;
    public Transform cameraLocationA;
    public Transform cameraLocationB;
    public Transform cameraLocationC;

    [Header("Secondary Camera Orthographic Sizes")]
    [Tooltip("Orthographic size when secondary camera teleports to Location A (Brain).")]
    public float cameraOrthoSizeA = 5f;
    [Tooltip("Orthographic size when secondary camera teleports to Location B (Muscle).")]
    public float cameraOrthoSizeB = 5f;
    [Tooltip("Orthographic size when secondary camera teleports to Location C (Heart).")]
    public float cameraOrthoSizeC = 5f;

    private int originalCullingMask;

    private void Start()
    {
        originalCullingMask = Camera.main.cullingMask;

        if (miniScreen != null)
            miniScreen.SetActive(false);

        // ✅ Player teleport buttons
        if (brainButton != null) brainButton.onClick.AddListener(() => TeleportPlayer(brainTarget));
        if (muscleButton != null) muscleButton.onClick.AddListener(() => TeleportPlayer(muscleTarget));
        if (heartButton != null) heartButton.onClick.AddListener(() => TeleportPlayer(heartTarget));

        // ✅ Camera teleport tied to player teleport buttons, each with its own ortho size
        if (brainButton != null) brainButton.onClick.AddListener(() => TeleportSecondaryCamera(cameraLocationA, cameraOrthoSizeA));
        if (muscleButton != null) muscleButton.onClick.AddListener(() => TeleportSecondaryCamera(cameraLocationB, cameraOrthoSizeB));
        if (heartButton != null) heartButton.onClick.AddListener(() => TeleportSecondaryCamera(cameraLocationC, cameraOrthoSizeC));
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

        // ✅ Default to Location A (with its size) on Execute
        if (teleportSecondaryCamera)
            TeleportSecondaryCamera(cameraLocationA, cameraOrthoSizeA);

        if (miniScreen != null)
            miniScreen.SetActive(true);
    }

    private void TeleportSecondaryCamera(Transform destination, float orthoSize)
    {
        if (!teleportSecondaryCamera) return;

        if (secondaryCamera == null)
        {
            Debug.LogWarning("Secondary camera not assigned!");
            return;
        }

        if (destination == null)
        {
            Debug.LogWarning("Camera destination not assigned!");
            return;
        }

        secondaryCamera.transform.position = destination.position;

        if (secondaryCamera.orthographic)
        {
            secondaryCamera.orthographicSize = orthoSize;
            Debug.Log($"Secondary camera teleported to '{destination.name}' with orthographic size {orthoSize}.");
        }
        else
        {
            Debug.LogWarning($"Secondary camera is not Orthographic — size not applied.");
        }
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