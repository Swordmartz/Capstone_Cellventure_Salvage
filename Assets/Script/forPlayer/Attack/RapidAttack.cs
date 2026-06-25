using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RapidAttackStackUI : MonoBehaviour
{
    [Header("References")]
    public SpearMeleeAttack spearAttack;
    public Slider stackSlider;
    public Button stackButton;
    [Tooltip("Drag in the player's movement script so it can be locked during the rapid attack burst.")]
    public PlayerMovementTry playerMovement;
    [Tooltip("Drag in the player's Rigidbody. Its velocity is forced to zero every frame during the burst, " +
             "regardless of which movement script is currently driving it.")]
    public Rigidbody playerRigidbody;
    [Tooltip("Drag in the player's SplineExplorerPlayer. Its speed fields are zeroed during the burst " +
             "and restored to their original values afterward.")]
    public SplineExplorerPlayer splineExplorer;
    [Tooltip("Optional. If assigned, each enemy hit during the rapid attack also adds combo charge " +
             "to the super attack bar.")]
    public SuperAttackBarUI superAttackBar;

    [Header("Settings")]
    public int maxStack = 5;

    [Header("Rapid Attack Burst")]
    [Tooltip("Total duration of the rapid attack burst, in seconds.")]
    public float rapidAttackDuration = 2f;
    [Tooltip("Time between each attack call during the burst. Should be >= spearAttack's meleeCooldown " +
             "or some hits will be silently skipped by its own cooldown check.")]
    public float rapidAttackInterval = 0.3f;

    [Header("Enemy Freeze")]
    [Tooltip("How long (in seconds) an enemy hit during the rapid attack is frozen for. " +
             "Refreshes on each tick that hits it, so an enemy caught in the burst stays " +
             "locked the whole time hits keep landing.")]
    public float enemyFreezeDuration = 2f;

    private bool isRapidAttacking = false;

    void Start()
    {
        if (stackSlider != null)
        {
            stackSlider.minValue = 0;
            stackSlider.maxValue = maxStack;
        }

        if (stackButton != null)
            stackButton.onClick.AddListener(OnStackButtonClicked);

        RefreshUI();
    }

    void Update()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (spearAttack == null) return;

        int currentStack = Mathf.Min(spearAttack.rapidAttackStack, maxStack);

        if (stackSlider != null)
            stackSlider.value = currentStack;

        if (stackButton != null)
            stackButton.interactable = currentStack >= maxStack && !isRapidAttacking;
    }

    private void OnStackButtonClicked()
    {
        if (spearAttack == null || isRapidAttacking) return;
        if (spearAttack.rapidAttackStack < maxStack) return;

        Debug.Log("[RapidAttackStackUI] Button pressed — starting rapid attack burst.");
        StartCoroutine(RapidAttackRoutine());
    }

    private IEnumerator RapidAttackRoutine()
    {
        isRapidAttacking = true;

        if (playerMovement != null)
            playerMovement.SetAttackLock(true);

        Coroutine freezeRoutine = null;
        if (playerRigidbody != null)
            freezeRoutine = StartCoroutine(FreezeRigidbodyRoutine());

        // Remember original speeds and zero them out so SplineExplorerPlayer
        // never calculates a nonzero desiredVel in the first place.
        float originalForwardSpeed = 0f, originalBackwardSpeed = 0f, originalSideSpeed = 0f;
        if (splineExplorer != null)
        {
            originalForwardSpeed = splineExplorer.forwardSpeed;
            originalBackwardSpeed = splineExplorer.backwardSpeed;
            originalSideSpeed = splineExplorer.sideSpeed;

            splineExplorer.forwardSpeed = 0f;
            splineExplorer.backwardSpeed = 0f;
            splineExplorer.sideSpeed = 0f;
        }

        float elapsed = 0f;

        while (elapsed < rapidAttackDuration)
        {
            spearAttack.PerformAttack();
            FreezeEnemiesInRange();

            yield return new WaitForSeconds(rapidAttackInterval);
            elapsed += rapidAttackInterval;
        }

        if (freezeRoutine != null)
            StopCoroutine(freezeRoutine);

        // Restore original speeds.
        if (splineExplorer != null)
        {
            splineExplorer.forwardSpeed = originalForwardSpeed;
            splineExplorer.backwardSpeed = originalBackwardSpeed;
            splineExplorer.sideSpeed = originalSideSpeed;
        }

        if (playerMovement != null)
            playerMovement.SetAttackLock(false);

        // Stack is intentionally NOT reset — stays at maxStack so the
        // button remains active/usable indefinitely after first fill.

        isRapidAttacking = false;
    }

    /// <summary>
    /// Forces the player's horizontal velocity to zero every physics step
    /// while the rapid attack is running — overriding any movement script
    /// (PlayerMovementTry, SplineExplorerPlayer, or anything else) that may
    /// be driving this Rigidbody, without needing to modify those scripts.
    /// </summary>
    private IEnumerator FreezeRigidbodyRoutine()
    {
        while (true)
        {
            yield return new WaitForFixedUpdate();

            Vector3 vel = playerRigidbody.linearVelocity;
            vel.x = 0f;
            vel.z = 0f;
            playerRigidbody.linearVelocity = vel;
        }
    }

    /// <summary>
    /// Rebuilds the same capsule shape SpearMeleeAttack uses for its hit
    /// check, purely to find enemies in range and freeze them — kept
    /// entirely separate from SpearMeleeAttack so that script doesn't need
    /// to be modified.
    /// </summary>
    private void FreezeEnemiesInRange()
    {
        if (spearAttack == null) return;

        Vector3 attackDir = (playerMovement != null && playerMovement.lastInputDirection.sqrMagnitude > 0.01f)
            ? playerMovement.lastInputDirection.normalized
            : spearAttack.transform.forward;

        Vector3 capsuleStart = spearAttack.transform.position;
        Vector3 capsuleEnd = capsuleStart + attackDir * spearAttack.attackRange;

        Collider[] hits = Physics.OverlapCapsule(
            capsuleStart, capsuleEnd, spearAttack.capsuleRadius, spearAttack.enemyLayer);

        foreach (Collider hit in hits)
        {
            EnemyFSM enemy = hit.GetComponentInParent<EnemyFSM>();
            if (enemy == null) continue;

            enemy.Freeze(enemyFreezeDuration);

            if (superAttackBar != null)
                superAttackBar.AddCombo();
        }
    }
}