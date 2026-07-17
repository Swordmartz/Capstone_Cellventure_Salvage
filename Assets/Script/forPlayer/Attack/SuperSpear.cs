using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The player's super attack: hits all enemies in a circle (sphere) centered
/// on the player, applying a damage-over-time effect (capable of killing
/// them) plus a movement slow for the same duration.
/// </summary>
public class SuperAttack : MonoBehaviour
{
    [Header("References")]
    public LayerMask enemyLayer;

    [Header("Area")]
    [Tooltip("Radius of the circle/sphere centered on the player.")]
    public float radius = 4f;

    [Header("Damage Over Time")]
    [Tooltip("Total damage dealt to each enemy over the full duration.")]
    public int totalDamage = 50;
    [Tooltip("How long the DoT lasts, in seconds.")]
    public float dotDuration = 4f;
    [Tooltip("How often the DoT ticks, in seconds.")]
    public float tickInterval = 0.5f;

    [Header("Slow")]
    [Tooltip("Movement speed multiplier applied to hit enemies (0.5 = half speed).")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.5f;

    public Animator anim;

    /// <summary>
    /// Call this to fire the super attack. Returns the number of enemies hit.
    /// </summary>
    public int PerformSuperAttack()
    {
        if (anim != null)
        {
            anim.SetBool("IsSuper", true);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayer);

        var hitObjects = new HashSet<GameObject>();
        int count = 0;

        foreach (Collider hit in hits)
        {
            if (TryApplySuperAttack(hit, hitObjects))
                count++;
        }

        return count;
    }

    // Checks for EACH known enemy script type on the hit collider (or its
    // parents) and applies the DoT + slow to whichever one is found. Uses
    // hitObjects (keyed by GameObject) to dedupe across enemy types since
    // EnemyFSM and EnemyPatrolFSM don't share a common base type.
    private bool TryApplySuperAttack(Collider hit, HashSet<GameObject> hitObjects)
    {
        EnemyFSM enemy = hit.GetComponentInParent<EnemyFSM>();
        if (enemy != null)
        {
            if (hitObjects.Contains(enemy.gameObject)) return false;
            hitObjects.Add(enemy.gameObject);

            enemy.ApplyDamageOverTime(totalDamage, dotDuration, tickInterval);
            enemy.ApplySlow(slowMultiplier, dotDuration);
            return true;
        }

        EnemyPatrolFSM patrolEnemy = hit.GetComponentInParent<EnemyPatrolFSM>();
        if (patrolEnemy != null)
        {
            if (hitObjects.Contains(patrolEnemy.gameObject)) return false;
            hitObjects.Add(patrolEnemy.gameObject);

            patrolEnemy.ApplyDamageOverTime(totalDamage, dotDuration, tickInterval);
            patrolEnemy.ApplySlow(slowMultiplier, dotDuration);
            return true;
        }

        return false;
    }

    public void FinishAttackSuper()
    {      
            anim.SetBool("IsSuper", false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }


}