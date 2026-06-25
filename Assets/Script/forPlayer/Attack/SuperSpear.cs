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

    /// <summary>
    /// Call this to fire the super attack. Returns the number of enemies hit.
    /// </summary>
    public int PerformSuperAttack()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayer);

        var hitEnemies = new HashSet<EnemyFSM>();
        int count = 0;

        foreach (Collider hit in hits)
        {
            EnemyFSM enemy = hit.GetComponentInParent<EnemyFSM>();
            if (enemy == null) continue;
            if (hitEnemies.Contains(enemy)) continue;

            hitEnemies.Add(enemy);
            enemy.ApplyDamageOverTime(totalDamage, dotDuration, tickInterval);
            enemy.ApplySlow(slowMultiplier, dotDuration);
            count++;
        }

        return count;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}