using UnityEngine;

/// <summary>
/// The player's eat skill: an execute-style attack that instantly finishes
/// off a living enemy positioned in front of the player, but ONLY if that
/// enemy's current HP is already at or below eatHpThreshold. Enemies above
/// the threshold are too healthy to be eaten and the attack does nothing.
///
/// "In front" reuses PlayerMovementTry's existing lastInputDirection —
/// the last direction real movement input was given, held even after the
/// stick is released — instead of tracking a separate copy of facing here.
///
/// Checks BOTH known enemy script types (DetectionFSM and EnemyFSM) on
/// whatever is hit, since they don't share a common base type in this
/// project (same dual-check pattern as SuperAttack).
/// </summary>
public class EatSkill : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's movement script. Its lastInputDirection is reused here as the eat-attack's facing direction.")]
    public PlayerMovementTry playerMovement;
    [Tooltip("Layer(s) enemies live on.")]
    public LayerMask enemyLayer;
    public Animator anim;

    [Header("Eat Requirement")]
    [Tooltip("An enemy must have currentHealth at or below this value to be eaten. " +
             "Enemies above this threshold cannot be eaten.")]
    public int eatHpThreshold = 30;


    [Header("Range")]
    [Tooltip("How far in front of the player to search for an eatable enemy.")]
    public float eatRange = 2f;
    [Tooltip("Radius of the search area at that forward point.")]
    public float eatRadius = 1.2f;

    /// <summary>
    /// Current facing direction, sourced from PlayerMovementTry.lastInputDirection.
    /// Falls back to transform.forward if playerMovement isn't assigned.
    /// </summary>
    private Vector3 FacingDir
    {
        get
        {
            Vector3 dir = playerMovement != null ? playerMovement.lastInputDirection : transform.forward;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
        }
    }

    /// <summary>
    /// Void wrapper around PerformEat() for UI Button OnClick() events.
    /// Unity's OnClick() dropdown only lists methods that return void, so
    /// PerformEat() (which returns bool) doesn't appear there on its own.
    /// Hook THIS method up to the button instead.
    /// </summary>
    public void PerformEatButton()
    {
        PerformEat();
    }

    /// <summary>
    /// Call this to attempt the eat attack. Returns true if an enemy was
    /// successfully eaten, false if there was no enemy in range, or the
    /// nearest one in front was above the HP threshold.
    /// </summary>
    public bool PerformEat()
    {
        Vector3 searchCenter = transform.position + FacingDir * eatRange;
        Collider[] hits = Physics.OverlapSphere(searchCenter, eatRadius, enemyLayer);

        GameObject bestTarget = null;
        float bestDist = float.MaxValue;
        int bestHealth = int.MaxValue;

        foreach (Collider hit in hits)
        {
            if (!TryGetEnemyHealth(hit, out GameObject enemyObj, out int health))
                continue;

            float dist = Vector3.Distance(transform.position, enemyObj.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTarget = enemyObj;
                bestHealth = health;
            }
        }

        if (bestTarget == null)
            return false; // Nothing eatable in range

        if (bestHealth > eatHpThreshold)
        {
            Debug.Log($"{bestTarget.name} is too healthy to eat ({bestHealth} HP, needs \u2264 {eatHpThreshold}).");
            return false;
        }

        if (anim != null)
        {
            anim.SetTrigger("Eat"); // Adjust to SetBool(...) if your Animator uses a bool instead
            anim.SetBool("IsNEating", true); // Tick before the eat itself happens
        }

        EatTarget(bestTarget);
        return true;
    }

    // Checks EACH known enemy script type on the hit collider (or its
    // parents) and returns whichever one is found, along with its current
    // health. Skips enemies already in a Dead state.
    private bool TryGetEnemyHealth(Collider hit, out GameObject enemyObj, out int health)
    {
        DetectionFSM detectionEnemy = hit.GetComponentInParent<DetectionFSM>();
        if (detectionEnemy != null)
        {
            if (detectionEnemy.currentState == DetectionFSM.EnemyState.Dead)
            {
                enemyObj = null;
                health = 0;
                return false;
            }

            enemyObj = detectionEnemy.gameObject;
            health = detectionEnemy.currentHealth;
            return true;
        }

        EnemyFSM enemyFsm = hit.GetComponentInParent<EnemyFSM>();
        if (enemyFsm != null)
        {
            enemyObj = enemyFsm.gameObject;
            health = enemyFsm.currentHealth;
            return true;
        }

        enemyObj = null;
        health = 0;
        return false;
    }

    // Sets HP to 0 and disables the GameObject directly, bypassing each
    // enemy script's normal Die()/SuperKill() flow (no death animation,
    // no HP bar hide, no mission-complete callback). If you want those
    // side effects too, call detectionEnemy.ForceKill() / enemyFsm.SuperKill()
    // here instead of setting currentHealth and disabling manually.
    private void EatTarget(GameObject enemyObj)
    {
        DetectionFSM detectionEnemy = enemyObj.GetComponent<DetectionFSM>();
        if (detectionEnemy != null)
            detectionEnemy.currentHealth = 0;

        EnemyFSM enemyFsm = enemyObj.GetComponent<EnemyFSM>();
        if (enemyFsm != null)
            enemyFsm.currentHealth = 0;

        enemyObj.SetActive(false);
    }
    public void FinishNEating()
    {
        anim.SetBool("IsNEating", false);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Vector3 searchCenter = transform.position + FacingDir * eatRange;
        Gizmos.DrawWireSphere(searchCenter, eatRadius);
        Gizmos.DrawLine(transform.position, searchCenter);
    }
}