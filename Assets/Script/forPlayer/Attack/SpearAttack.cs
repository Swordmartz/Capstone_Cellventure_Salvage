using UnityEngine;

public class SpearMeleeAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float capsuleRadius = 0.3f;
    public float capsuleHeight = 2f;
    public LayerMask enemyLayer;
    public int attackDamage = 1; // ✅ Add this

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

    public void PerformAttack()
    {
        if (Time.time < lastMeleeTime + meleeCooldown) return;

        lastMeleeTime = Time.time;

        Vector3 attackDir = (playerMovement != null && playerMovement.lastInputDirection.sqrMagnitude > 0.01f)
            ? playerMovement.lastInputDirection
            : transform.forward;

        Vector3 capsuleStart = transform.position;
        Vector3 capsuleEnd = transform.position + attackDir * attackRange;

        Collider[] hits = Physics.OverlapCapsule(capsuleStart, capsuleEnd, capsuleRadius, enemyLayer);

        foreach (Collider hit in hits)
        {
            EnemyFSM enemy = hit.GetComponent<EnemyFSM>();
            if (enemy == null) continue;

            // ✅ Actually deal damage first
            enemy.TakeDamage(attackDamage);

            // ✅ Then disable if dead
            if (enemy.currentHealth <= 0)
                hit.gameObject.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 dir = Application.isPlaying
            ? (playerMovement?.lastInputDirection.sqrMagnitude > 0.01f == true
                ? playerMovement.lastInputDirection
                : transform.forward)
            : transform.forward;

        Vector3 capsuleStart = transform.position;
        Vector3 capsuleEnd = transform.position + dir * attackRange;

        // Draw the capsule as two spheres + a line for editor visualization
        Gizmos.DrawWireSphere(capsuleStart, capsuleRadius);
        Gizmos.DrawWireSphere(capsuleEnd, capsuleRadius);
        Gizmos.DrawLine(capsuleStart + Vector3.up * capsuleRadius, capsuleEnd + Vector3.up * capsuleRadius);
        Gizmos.DrawLine(capsuleStart - Vector3.up * capsuleRadius, capsuleEnd - Vector3.up * capsuleRadius);
    }
}