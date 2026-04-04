using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    public int damage = 2;
    public float attackRange = 1.5f;
    public float attackRadius = 1f; // bigger radius for detection
    public LayerMask enemyLayer;

    private PlayerMovementTry playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementTry>();
    }

    // Called by UI Button OnClick()
    public void PerformAttack()
    {
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovementTry reference missing!");
            return;
        }

        // Use last joystick direction, fallback to forward
        Vector3 attackDir = playerMovement.lastInputDirection.sqrMagnitude > 0.01f
            ? playerMovement.lastInputDirection
            : transform.forward;

        Vector3 attackOrigin = transform.position + attackDir * attackRange;

        Collider[] hits = Physics.OverlapSphere(attackOrigin, attackRadius, enemyLayer);

        foreach (Collider hit in hits)
        {
            DetectionFSM enemy = hit.GetComponent<DetectionFSM>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"Hit {enemy.name} with melee attack!");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (playerMovement != null)
        {
            Vector3 attackDir = playerMovement.lastInputDirection.sqrMagnitude > 0.01f
                ? playerMovement.lastInputDirection
                : transform.forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + attackDir * attackRange, attackRadius);
        }
    }
}
