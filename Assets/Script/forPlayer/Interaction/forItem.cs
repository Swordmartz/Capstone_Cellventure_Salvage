using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
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

    [Header("Mini-Screen Teleport")]
    public GameObject miniScreen;
    public Button brainButton;
    public Button muscleButton;
    public Button heartButton;

    [Header("First Teleport Optional Deactivation")]
    [Tooltip("Optional GameObject to deactivate before the first teleport (Brain button).")]
    public GameObject objectToDeactivateOnFirstTP;

    [Header("Third Teleport Optional Activation")]
    [Tooltip("Optional GameObject to activate before the third teleport (Heart button).")]
    public GameObject objectToActivateOnThirdTP;

    [Tooltip("Assign the Player root or the Player object with the Player tag.")]
    public Transform player;

    [Header("Player Root Movement")]
    [Tooltip("Assign the top/root player object here. This is the object that will be moved so the Rigidbody child reaches the target.")]
    public Transform playerRootToMove;

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
    [Tooltip("Orthographic size when secondary camera teleports to Location A / Brain.")]
    public float cameraOrthoSizeA = 5f;

    [Tooltip("Orthographic size when secondary camera teleports to Location B / Muscle.")]
    public float cameraOrthoSizeB = 5f;

    [Tooltip("Orthographic size when secondary camera teleports to Location C / Heart.")]
    public float cameraOrthoSizeC = 5f;

    [Header("Mission Submission")]
    [Tooltip("Enable this to complete a mission when Execute() is called.")]
    public bool completeMissionOnExecute = false;
    [Tooltip("Which mission index to complete (0 = first, 1 = second, etc.)")]
    public int missionIndex = 0;
    public MissionSubmissionManager missionManager;

    [Header("Fade Screen")]
    [Tooltip("If disabled, the screen will not fade before/after the teleport.")]
    public bool useFadeOnTeleport = true;
    [Tooltip("Assign the RFadeManager used to fade the screen before/after teleports.")]
    public RFadeManager fadeManager;
    [Tooltip("How long the screen takes to fade to black before the teleport/video.")]
    public float fadeOutDuration = 1f;
    [Tooltip("How long the screen takes to fade back in after the video finishes.")]
    public float fadeInDuration = 1f;

    [Header("Teleport Cutscenes")]
    [Tooltip("If disabled, no cutscene video will play during the teleport.")]
    public bool useVideoOnTeleport = true;
    [Tooltip("VideoPlayer that plays the cutscene while the player is teleported.")]
    public VideoPlayer cutscenePlayer;
    [Tooltip("Optional GameObject (e.g. a RawImage/Canvas) that displays the video. Enabled while the video plays, disabled after.")]
    public GameObject cutsceneScreen;
    [Tooltip("Video clip played when teleporting to the Brain.")]
    public VideoClip brainVideoClip;
    [Tooltip("Video clip played when teleporting to the Muscle.")]
    public VideoClip muscleVideoClip;
    [Tooltip("Video clip played when teleporting to the Heart.")]
    public VideoClip heartVideoClip;

    [Header("Game Timer")]
    [Tooltip("Optional. If assigned, this timer is stopped the moment the cutscene/teleport " +
             "sequence begins and resumed right after it finishes (fade back in included). " +
             "Leave unassigned to skip this entirely.")]
    public GameTimer gameTimer;

    [Header("Fade Screen Sort Order (Cutscene Fix)")]
    [Tooltip("Optional. The Canvas that the fade screen (fadeManager) renders on. If assigned, " +
             "its sortingOrder is dropped below the cutscene video right before the video plays, " +
             "so the fade doesn't cover it, then restored once the video finishes.")]
    public Canvas fadeCanvas;
    [Tooltip("Sorting order applied to fadeCanvas while the cutscene video is playing.")]
    public int fadeSortOrderDuringVideo = -1;
    [Tooltip("Sorting order fadeCanvas is restored to once the cutscene video finishes.")]
    public int fadeSortOrderDefault = 100;

    private int originalCullingMask;

    private void Start()
    {
        if (Camera.main != null)
        {
            originalCullingMask = Camera.main.cullingMask;
        }

        if (miniScreen != null)
        {
            miniScreen.SetActive(false);
        }

        if (cutsceneScreen != null)
        {
            cutsceneScreen.SetActive(false);
        }

        if (brainButton != null)
        {
            Transform brain = brainTarget;
            Transform camA = cameraLocationA;
            float orthoA = cameraOrthoSizeA;

            brainButton.onClick.AddListener(() =>
            {
                if (objectToDeactivateOnFirstTP != null)
                    objectToDeactivateOnFirstTP.SetActive(false);

                StartCoroutine(TeleportWithFadeAndVideo(brain, camA, orthoA, brainVideoClip));
            });
        }

        if (muscleButton != null)
        {
            Transform muscle = muscleTarget;
            Transform camB = cameraLocationB;
            float orthoB = cameraOrthoSizeB;

            muscleButton.onClick.AddListener(() =>
            {
                StartCoroutine(TeleportWithFadeAndVideo(muscle, camB, orthoB, muscleVideoClip));
            });
        }

        if (heartButton != null)
        {
            Transform heart = heartTarget;
            Transform camC = cameraLocationC;
            float orthoC = cameraOrthoSizeC;

            heartButton.onClick.AddListener(() =>
            {
                if (objectToActivateOnThirdTP != null)
                    objectToActivateOnThirdTP.SetActive(true);

                StartCoroutine(TeleportWithFadeAndVideo(heart, camC, orthoC, heartVideoClip));
            });
        }
    }

    /// <summary>
    /// Fades the screen out, plays the cutscene video, teleports the player (and secondary
    /// camera) while the video plays, then stops the video and fades the screen back in.
    /// The assigned gameTimer (if any) is paused for the full duration of this sequence and
    /// resumed right at the end.
    /// </summary>
    private IEnumerator TeleportWithFadeAndVideo(Transform playerDestination, Transform camDestination, float orthoSize, VideoClip cutsceneClip)
    {
        // 0) Pause the timer for the duration of the cutscene/teleport sequence (optional).
        bool timerWasActive = false;
        if (gameTimer != null)
        {
            timerWasActive = gameTimer.timerActive;
            if (timerWasActive)
            {
                gameTimer.StopTimer();
                Debug.Log("[Item] Paused gameTimer for teleport/cutscene sequence.");
            }
        }

        // 1) Fade the screen out first (optional).
        if (useFadeOnTeleport)
        {
            if (fadeManager != null)
            {
                fadeManager.DoFade(0f, 1f, fadeOutDuration, 0f);
                yield return new WaitForSeconds(fadeOutDuration);
            }
            else
            {
                Debug.LogWarning("[Item] useFadeOnTeleport is true but fadeManager is not assigned - skipping fade.");
            }
        }

        // 2) Start the cutscene video (optional; screen is black/faded at this point if fade was used).
        bool videoPlaying = false;
        if (useVideoOnTeleport && cutscenePlayer != null && cutsceneClip != null)
        {
            if (cutsceneScreen != null)
                cutsceneScreen.SetActive(true);

            cutscenePlayer.clip = cutsceneClip;
            cutscenePlayer.Prepare();

            while (!cutscenePlayer.isPrepared)
                yield return null;

            // Drop the fade screen's sort order so it stops covering the video.
            if (fadeCanvas != null)
            {
                fadeCanvas.sortingOrder = fadeSortOrderDuringVideo;
            }

            cutscenePlayer.Play();
            videoPlaying = true;
        }
        else if (useVideoOnTeleport)
        {
            Debug.LogWarning("[Item] useVideoOnTeleport is true but cutscenePlayer or video clip is not assigned - skipping video.");
        }

        // 3) Teleport the player and secondary camera while the video plays.
        TeleportPlayerToPosition(playerDestination);
        TeleportSecondaryCamera(camDestination, orthoSize);

        if (enableFloorVisibility)
        {
            ApplyLayerVisibility();
        }

        if (miniScreen != null)
        {
            miniScreen.SetActive(false);
        }

        // 4) Wait for the video to finish playing, then stop it.
        if (videoPlaying)
        {
            while (cutscenePlayer.isPlaying)
                yield return null;

            cutscenePlayer.Stop();

            if (cutsceneScreen != null)
                cutsceneScreen.SetActive(false);

            // Restore the fade screen's sort order now that the video is done.
            if (fadeCanvas != null)
            {
                fadeCanvas.sortingOrder = fadeSortOrderDefault;
            }
        }

        // 5) Fade the screen back in now that the teleport/video is done (optional).
        if (useFadeOnTeleport && fadeManager != null)
        {
            fadeManager.DoFade(1f, 0f, fadeInDuration, 0f);
        }

        // 6) Resume the timer now that the sequence is fully done (optional).
        if (gameTimer != null && timerWasActive)
        {
            gameTimer.ResumeTimer();
            Debug.Log("[Item] Resumed gameTimer after teleport/cutscene sequence.");
        }
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

            if (!playerInventory.HasItem)
            {
                Debug.Log("No item in inventory — playing dialogue.");
                if (AI_Test != null)
                    StartCoroutine(AI_Test.DialogueSequence3IRBC());
                return;
            }
        }

        if (AI_Test != null && guideSystem != null)
        {
            guideSystem.guideEnabled = false;
        }

        if (optionalObjectToDisable != null)
        {
            optionalObjectToDisable.SetActive(false);
        }

        if (enableFloorVisibility)
        {
            ApplyLayerVisibility();
        }

        if (teleportTarget != null)
        {
            if (optionalObjectToDisable != null)
                optionalObjectToDisable.SetActive(false);

            TeleportPlayerToPosition(teleportTarget);
        }

        if (teleportSecondaryCamera)
        {
            TeleportSecondaryCamera(cameraLocationA, cameraOrthoSizeA);
        }

        if (miniScreen != null)
        {
            miniScreen.SetActive(true);
        }

        if (completeMissionOnExecute)
        {
            if (missionManager != null)
            {
                missionManager.CompleteMissionByIndex(missionIndex);
                Debug.Log("[Item] Mission " + missionIndex + " completed via Execute().");
            }
            else
            {
                Debug.LogWarning("[Item] completeMissionOnExecute is true but missionManager is not assigned.");
            }
        }
    }

    private Transform GetPlayerToTeleport()
    {
        if (player != null && player.gameObject.activeInHierarchy)
        {
            Rigidbody assignedRb = FindRigidbodyForPlayer(player);

            if (assignedRb != null)
            {
                Debug.Log("Using assigned active player: " + player.name + " | Rigidbody: " + assignedRb.name);
                return player;
            }

            Debug.LogWarning("Assigned player is active but has no Rigidbody on itself, child, or parent: " + player.name);
        }

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject playerObj in players)
        {
            if (playerObj == null || !playerObj.activeInHierarchy)
                continue;

            Rigidbody rb = FindRigidbodyForPlayer(playerObj.transform);

            if (rb != null)
            {
                Debug.Log("Using active Player tag object: " + playerObj.name + " | Rigidbody: " + rb.name);
                return playerObj.transform;
            }

            Debug.LogWarning("Skipping Player-tagged object with no Rigidbody on itself, child, or parent: " + playerObj.name);
        }

        Debug.LogWarning("No active Player object with Rigidbody found.");
        return null;
    }

    private Rigidbody FindRigidbodyForPlayer(Transform selectedPlayer)
    {
        if (selectedPlayer == null)
            return null;

        Rigidbody rb = selectedPlayer.GetComponent<Rigidbody>();

        if (rb == null)
            rb = selectedPlayer.GetComponentInChildren<Rigidbody>();

        if (rb == null)
            rb = selectedPlayer.GetComponentInParent<Rigidbody>();

        return rb;
    }

    private void TeleportPlayerToPosition(Transform destination)
    {
        if (destination == null)
        {
            Debug.LogWarning("Teleport destination not assigned.");
            return;
        }

        Transform selectedPlayer = GetPlayerToTeleport();

        if (selectedPlayer == null)
            return;

        Rigidbody rb = FindRigidbodyForPlayer(selectedPlayer);

        if (rb == null)
        {
            Debug.LogWarning("No Rigidbody found for selected player: " + selectedPlayer.name);
            return;
        }

        Transform rootToMove = playerRootToMove != null ? playerRootToMove : selectedPlayer;

        Vector3 targetPosition = destination.position;
        Vector3 offsetNeeded = targetPosition - rb.transform.position;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rootToMove.position += offsetNeeded;

        rb.position = targetPosition;
        rb.transform.position = targetPosition;

        Physics.SyncTransforms();

        Debug.Log(
            "Teleported root: " + rootToMove.name +
            " | Rigidbody: " + rb.name +
            " | Target: " + destination.name +
            " at " + targetPosition +
            " | Offset applied: " + offsetNeeded
        );
    }

    private void TeleportPlayer(Transform destination)
    {
        TeleportPlayerToPosition(destination);

        if (enableFloorVisibility)
        {
            ApplyLayerVisibility();
        }

        if (miniScreen != null)
        {
            miniScreen.SetActive(false);
        }
    }

    private void TeleportSecondaryCamera(Transform destination, float orthoSize)
    {
        if (!teleportSecondaryCamera)
        {
            return;
        }

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
            Debug.Log("Secondary camera teleported to " + destination.name + " with orthographic size " + orthoSize);
        }
        else
        {
            Debug.LogWarning("Secondary camera is not Orthographic. Size not applied.");
        }
    }

    private void ApplyLayerVisibility()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("Main Camera not found.");
            return;
        }

        int newMask = originalCullingMask;

        foreach (string layerName in layersToHide)
        {
            int layer = LayerMask.NameToLayer(layerName);

            Debug.Log("Hiding layer: " + layerName + " = index " + layer);

            if (layer == -1)
            {
                Debug.LogWarning("Layer '" + layerName + "' not found!");
                continue;
            }

            newMask &= ~(1 << layer);
        }

        Debug.Log("Old mask: " + originalCullingMask + " | New mask: " + newMask);

        Camera.main.cullingMask = newMask;
    }

    public void RestoreCullingMask()
    {
        if (Camera.main != null)
        {
            Camera.main.cullingMask = originalCullingMask;
        }
    }
}