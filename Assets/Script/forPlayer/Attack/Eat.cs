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
            if (enemy != null)
            {
                if (enemy.currentHealth <= 0)
                {
                    EatTarget(hit.gameObject, InfectionManager.FoodType.Enemy);
                }
                continue;
            }

            // InfluenzaFSM no longer deactivates itself on death (it stays in
            // place instead), so it needs its own dead-state check here —
            // once IsDead is true, it's eatable the same as a dead DetectionFSM.
            InfluenzaFSM influenzaEnemy = hit.GetComponent<InfluenzaFSM>();
            if (influenzaEnemy != null)
            {
                if (influenzaEnemy.IsDead)
                {
                    EatTarget(hit.gameObject, InfectionManager.FoodType.Enemy);
                }
                continue;
            }

            // pneumonococcalFSM also no longer destroys itself on death (it
            // stays in place too), so it gets the same IsDead check as
            // InfluenzaFSM — once IsDead is true, it's eatable the same way.
            pneumonococcalFSM pneumonococcalEnemy = hit.GetComponent<pneumonococcalFSM>();
            if (pneumonococcalEnemy != null)
            {
                if (pneumonococcalEnemy.IsDead)
                {
                    EatTarget(hit.gameObject, InfectionManager.FoodType.Enemy);
                }
                continue;
            }

            // EnemySplineFollower (dengue) also stays in place on death rather
            // than deactivating itself — IsDead there is currentHP <= 0, which
            // covers both "HP is 0" and "in the dead state" in one check, same
            // pattern as InfluenzaFSM/pneumonococcalFSM above.
            EnemySplineFollower dengueEnemy = hit.GetComponent<EnemySplineFollower>();
            if (dengueEnemy != null)
            {
                if (dengueEnemy.IsDead)
                {
                    EatTarget(hit.gameObject, InfectionManager.FoodType.Enemy);
                }
                continue;
            }

            // MalariaFSM also stays in place on death (0 HP -> State.Dead,
            // never disabled/destroyed) rather than deactivating itself, so it
            // gets the same IsDead check as the others above.
            MalariaFSM malariaEnemy = hit.GetComponent<MalariaFSM>();
            if (malariaEnemy != null && malariaEnemy.IsDead)
            {
                EatTarget(hit.gameObject, InfectionManager.FoodType.Enemy);
            }
        }
    }

    /// <summary>
    /// Shared "consume this target" logic: bumps the Infection meter, disables
    /// the GameObject, and — if it also has a DetectionFSM, InfluenzaFSM,
    /// pneumonococcalFSM, EnemySplineFollower, or MalariaFSM (i.e. it's an
    /// enemy, infected or not) — reports it to WinConditionManager the same
    /// as before.
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

        // InfluenzaFSM was already IsDead before it could be eaten (that's
        // the condition that made it eatable), so there's no death flag to
        // tick here — just report it the same way as a DetectionFSM kill.
        InfluenzaFSM influenzaEnemy = target.GetComponent<InfluenzaFSM>();
        if (influenzaEnemy != null)
        {
            if (WinConditionManager.Instance != null)
                WinConditionManager.Instance.ReportEnemyDefeated(target);
        }

        // Same story for pneumonococcalFSM — it was already IsDead before it
        // could be eaten, so just report the kill same as the others.
        pneumonococcalFSM pneumonococcalEnemy = target.GetComponent<pneumonococcalFSM>();
        if (pneumonococcalEnemy != null)
        {
            if (WinConditionManager.Instance != null)
                WinConditionManager.Instance.ReportEnemyDefeated(target);
        }

        // Same story for EnemySplineFollower (dengue) — already IsDead before
        // it could be eaten, so just report the kill same as the others.
        EnemySplineFollower dengueEnemy = target.GetComponent<EnemySplineFollower>();
        if (dengueEnemy != null)
        {
            if (WinConditionManager.Instance != null)
                WinConditionManager.Instance.ReportEnemyDefeated(target);
        }

        // Same story for MalariaFSM — already IsDead before it could be
        // eaten, so just report the kill same as the others.
        MalariaFSM malariaEnemy = target.GetComponent<MalariaFSM>();
        if (malariaEnemy != null)
        {
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