using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperEat : MonoBehaviour
{
    [Header("References")]
    public SliderTimer superBar;

    [Header("Suck Settings")]
    public float suckRadius = 40f;
    public float suckSpeed = 10f;

    [Header("Eat Capsule Detection")]
    public float capsuleRadius = 1.5f;
    public float capsuleHeight = 3f;
    public Vector3 capsuleOffset = new Vector3(0f, 0f, 0f);

    [Header("Duration")]
    public float eatDuration = 3f;

    private bool isEating = false;

    public Animator anim;
    public void ActivateSuperEat()
    {
        if (!superBar.IsFull) return;

        List<GameObject> targets = GetEatableTargetsInRadius();
        if (targets.Count == 0) return;

        if (anim != null)
            anim.SetBool("Super", true);

        StartCoroutine(SuckTargets(targets));
        superBar.ConsumeBar();
    }

    /// <summary>
    /// Gathers everything within suckRadius that's currently eatable:
    /// dead DetectionFSM enemies, dead InfluenzaFSM enemies, dead
    /// pneumonococcalFSM enemies, and dead MalariaFSM enemies (original
    /// behavior, extended to cover the extra enemy types), plus any
    /// InfectedCell that's currently infected — infected cells don't need
    /// to be "dead" first, being infected is enough.
    /// </summary>
    private List<GameObject> GetEatableTargetsInRadius()
    {
        List<GameObject> targets = new List<GameObject>();
        Collider[] hits = Physics.OverlapSphere(transform.position, suckRadius, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            InfectedCell infectedCell = hit.GetComponent<InfectedCell>();
            if (infectedCell != null && infectedCell.IsInfected)
            {
                targets.Add(hit.gameObject);
                continue;
            }

            DetectionFSM enemy = hit.GetComponent<DetectionFSM>();
            if (enemy != null && enemy.currentState == DetectionFSM.EnemyState.Dead)
            {
                targets.Add(hit.gameObject);
                continue;
            }

            InfluenzaFSM influenzaEnemy = hit.GetComponent<InfluenzaFSM>();
            if (influenzaEnemy != null && influenzaEnemy.IsDead)
            {
                targets.Add(hit.gameObject);
                continue;
            }

            pneumonococcalFSM pneumonococcalEnemy = hit.GetComponent<pneumonococcalFSM>();
            if (pneumonococcalEnemy != null && pneumonococcalEnemy.IsDead)
            {
                targets.Add(hit.gameObject);
                continue;
            }

            MalariaFSM malariaEnemy = hit.GetComponent<MalariaFSM>();
            if (malariaEnemy != null && malariaEnemy.IsDead)
                targets.Add(hit.gameObject);
        }

        return targets;
    }

    public void FinishSuper()
    {
        anim.SetBool("Super", false);
    }

    private IEnumerator SuckTargets(List<GameObject> targets)
    {
        isEating = true;
        float elapsed = 0f;

        List<GameObject> active = new List<GameObject>(targets);

        while (active.Count > 0 && elapsed < eatDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                GameObject target = active[i];

                if (target == null || !target.activeSelf)
                {
                    active.RemoveAt(i);
                    continue;
                }

                target.transform.position = Vector3.MoveTowards(
                    target.transform.position,
                    transform.position,
                    suckSpeed * Time.deltaTime
                );

                if (IsInsideCapsule(target.transform.position))
                {
                    EatTarget(target);
                    active.RemoveAt(i);
                }
            }

            yield return null;
        }

        isEating = false;
    }

    /// <summary>
    /// Shared "consume this target" logic: bumps the Infection meter (using
    /// InfectedCell food type if this was an infected cell, Enemy otherwise),
    /// disables the GameObject, and — if it also has a DetectionFSM,
    /// InfluenzaFSM, pneumonococcalFSM, or MalariaFSM — marks it dead
    /// (DetectionFSM only; the others are already dead by the time they're
    /// eatable) and reports it to WinConditionManager the same as before.
    /// </summary>
    private void EatTarget(GameObject target)
    {
        InfectedCell infectedCell = target.GetComponent<InfectedCell>();
        InfectionManager.FoodType foodType = (infectedCell != null && infectedCell.IsInfected)
            ? InfectionManager.FoodType.InfectedCell
            : InfectionManager.FoodType.Enemy;

        if (InfectionManager.Instance != null)
            InfectionManager.Instance.RegisterEaten(foodType);

        target.SetActive(false);

        DetectionFSM enemy = target.GetComponent<DetectionFSM>();
        if (enemy != null)
        {
            // Tick IsDead now that this enemy has been successfully
            // disabled (eaten), then let WinConditionManager know.
            enemy.isDead = true;

            if (WinConditionManager.Instance != null)
                WinConditionManager.Instance.ReportEnemyDefeated(target);
        }

        InfluenzaFSM influenzaEnemy = target.GetComponent<InfluenzaFSM>();
        if (influenzaEnemy != null)
        {
            // InfluenzaFSM is only ever gathered as a target once it's
            // already dead (see GetEatableTargetsInRadius), and IsDead has
            // no public setter, so there's nothing to flip here — just
            // report the kill to WinConditionManager same as DetectionFSM.
            if (WinConditionManager.Instance != null)
                WinConditionManager.Instance.ReportEnemyDefeated(target);
        }

        pneumonococcalFSM pneumonococcalEnemy = target.GetComponent<pneumonococcalFSM>();
        if (pneumonococcalEnemy != null)
        {
            // Same story as InfluenzaFSM — already dead by the time it's
            // eatable, and IsDead has no public setter, so just report it.
            if (WinConditionManager.Instance != null)
                WinConditionManager.Instance.ReportEnemyDefeated(target);
        }

        MalariaFSM malariaEnemy = target.GetComponent<MalariaFSM>();
        if (malariaEnemy != null)
        {
            // Same story as InfluenzaFSM/pneumonococcalFSM — already dead by
            // the time it's eatable, and IsDead has no public setter, so
            // just report it.
            if (WinConditionManager.Instance != null)
                WinConditionManager.Instance.ReportEnemyDefeated(target);
        }
    }

    private bool IsInsideCapsule(Vector3 point)
    {
        Vector3 center = transform.position + capsuleOffset;
        float halfHeight = Mathf.Max(0f, (capsuleHeight / 2f) - capsuleRadius);

        Vector3 pointA = center + Vector3.up * halfHeight;
        Vector3 pointB = center - Vector3.up * halfHeight;

        Vector3 ab = pointB - pointA;
        Vector3 ap = point - pointA;

        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Vector3.Dot(ab, ab));
        Vector3 closest = pointA + t * ab;

        return (point - closest).sqrMagnitude <= capsuleRadius * capsuleRadius;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, suckRadius);

        Vector3 center = transform.position + capsuleOffset;
        float halfHeight = Mathf.Max(0f, (capsuleHeight / 2f) - capsuleRadius);
        Vector3 pointA = center + Vector3.up * halfHeight;
        Vector3 pointB = center - Vector3.up * halfHeight;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pointA, capsuleRadius);
        Gizmos.DrawWireSphere(pointB, capsuleRadius);
        Gizmos.DrawLine(pointA + Vector3.left * capsuleRadius, pointB + Vector3.left * capsuleRadius);
        Gizmos.DrawLine(pointA + Vector3.right * capsuleRadius, pointB + Vector3.right * capsuleRadius);
        Gizmos.DrawLine(pointA + Vector3.forward * capsuleRadius, pointB + Vector3.forward * capsuleRadius);
        Gizmos.DrawLine(pointA + Vector3.back * capsuleRadius, pointB + Vector3.back * capsuleRadius);
    }
}