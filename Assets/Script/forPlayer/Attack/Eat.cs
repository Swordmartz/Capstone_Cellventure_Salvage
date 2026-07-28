using UnityEngine;

public class MeleeAttack2 : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 1.5f;
    public float attackRadius = 1f;
    public LayerMask enemyLayer;

    [Header("Cooldown")]
    public float meleeCooldown = 1f;

    private float lastMeleeTime = -999f;
    private PlayerMovementTry playerMovement;

    public Animator anim;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementTry>();

        if (playerMovement == null)
            Debug.LogError($"[MeleeAttack2] PlayerMovementTry not found on {gameObject.name}!");
    }

    public void PerformAttack()
    {
        if (anim != null)
            anim.SetBool("IsEating", true);

        if (Time.time < lastMeleeTime + meleeCooldown) return;

        lastMeleeTime = Time.time;

        Vector3 attackDir = (playerMovement != null && playerMovement.lastInputDirection.sqrMagnitude > 0.01f)
            ? playerMovement.lastInputDirection
            : transform.forward;

        Vector3 attackOrigin = transform.position + attackDir * attackRange;

        Collider[] hits = Physics.OverlapSphere(attackOrigin, attackRadius, enemyLayer);

        foreach (Collider hit in hits)
        {
            // Infected cells can be eaten directly — being infected is enough,
            // they don't need to be "dead" first the way a regular enemy does.
            InfectedCell infectedCell = hit.GetComponent<InfectedCell>();
            if (infectedCell != null && infectedCell.IsInfected)
            {
                EatTarget(hit.gameObject, InfectionManager.FoodType.InfectedCell);
                continue;
            }

            DetectionFSM enemy = hit.GetComponent<DetectionFSM>();
            if (enemy == null) continue;

            if (enemy.currentHealth <= 0 || enemy.currentState == DetectionFSM.EnemyState.Dead)
            {
                EatTarget(hit.gameObject, InfectionManager.FoodType.Enemy);
            }
        }
    }

    /// <summary>
    /// Shared "consume this target" logic: bumps the Infection meter, disables
    /// the GameObject, and — if it also has a DetectionFSM (i.e. it's an
    /// enemy, infected or not) — marks it dead and reports it to
    /// WinConditionManager the same as before.
    /// </summary>
    private void EatTarget(GameObject target, InfectionManager.FoodType foodType)
    {
        if (InfectionManager.Instance != null)
            InfectionManager.Instance.RegisterEaten(foodType);

        target.SetActive(false);

        DetectionFSM enemy = target.GetComponent<DetectionFSM>();
        if (enemy != null)
        {
            // Tick IsDead now that the GameObject has been successfully disabled.
            enemy.isDead = true;

            if (WinConditionManager.Instance != null)
                WinConditionManager.Instance.ReportEnemyDefeated(target);
        }
    }

    public void FinishMEat()
    {
        anim.SetBool("IsEating", false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Vector3 dir = Application.isPlaying
            ? (playerMovement?.lastInputDirection.sqrMagnitude > 0.01f == true
                ? playerMovement.lastInputDirection
                : transform.forward)
            : transform.forward;
        Gizmos.DrawWireSphere(transform.position + dir * attackRange, attackRadius);
    }
}