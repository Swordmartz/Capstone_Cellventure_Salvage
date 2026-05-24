using UnityEngine;

public class SpearMeleeAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float capsuleRadius = 0.3f;
    public LayerMask enemyLayer;
    public int attackDamage = 1;

    [Header("Cooldown")]
    public float meleeCooldown = 1f;

    private float lastMeleeTime = -999f;
    private PlayerMovementTry playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementTry>();

        if (playerMovement == null)
            Debug.LogError($"[SpearMeleeAttack] PlayerMovementTry not found on {gameObject.name}!");
    }

    // Returns the direction the player is facing/moving — never zero
    Vector3 GetAttackDirection()
    {
        if (playerMovement != null && playerMovement.lastInputDirection.sqrMagnitude > 0.01f)
            return playerMovement.lastInputDirection.normalized;

        // Fallback 1: use the transform's own forward
        if (transform.forward.sqrMagnitude > 0.01f)
            return transform.forward;

        // Fallback 2: just go positive Z
        return Vector3.forward;
    }

    public void PerformAttack()
    {
        if (Time.time < lastMeleeTime + meleeCooldown) return;
        lastMeleeTime = Time.time;

        Vector3 attackDir = GetAttackDirection();
        Vector3 capsuleStart = transform.position;
        Vector3 capsuleEnd = transform.position + attackDir * attackRange;

        Debug.Log($"[SpearMeleeAttack] Attacking — dir={attackDir}, start={capsuleStart}, end={capsuleEnd}");

        Collider[] hits = Physics.OverlapCapsule(capsuleStart, capsuleEnd, capsuleRadius, enemyLayer);

        Debug.Log($"[SpearMeleeAttack] Hits found: {hits.Length}");

        foreach (Collider hit in hits)
        {
            // GetComponentInParent handles colliders on child objects
            EnemyFSM enemy = hit.GetComponentInParent<EnemyFSM>();
            if (enemy == null)
            {
                Debug.Log($"[SpearMeleeAttack] Hit {hit.name} but no EnemyFSM found in parent chain.");
                continue;
            }

            Debug.Log($"[SpearMeleeAttack] Dealing {attackDamage} damage to {enemy.name}");
            enemy.TakeDamage(attackDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 dir = Application.isPlaying
            ? GetAttackDirection()
            : transform.forward;

        Vector3 capsuleStart = transform.position;
        Vector3 capsuleEnd = transform.position + dir * attackRange;

        Gizmos.color = Color.cyan;

        // Draw the full capsule length so you can see it is NOT a sphere
        Gizmos.DrawWireSphere(capsuleStart, capsuleRadius);
        Gizmos.DrawWireSphere(capsuleEnd, capsuleRadius);

        // Side lines connecting the two spheres — makes the capsule shape obvious
        Vector3 right = Vector3.Cross(dir, Vector3.up).normalized * capsuleRadius;
        Vector3 up = Vector3.up * capsuleRadius;

        Gizmos.DrawLine(capsuleStart + right, capsuleEnd + right);
        Gizmos.DrawLine(capsuleStart - right, capsuleEnd - right);
        Gizmos.DrawLine(capsuleStart + up, capsuleEnd + up);
        Gizmos.DrawLine(capsuleStart - up, capsuleEnd - up);

        // Center line showing attack direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(capsuleStart, capsuleEnd);
    }
}