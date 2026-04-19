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

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementTry>();

        if (playerMovement == null)
            Debug.LogError($"[MeleeAttack2] PlayerMovementTry not found on {gameObject.name}!");
    }

    public void PerformAttack()
    {
        if (Time.time < lastMeleeTime + meleeCooldown) return;

        lastMeleeTime = Time.time;

        Vector3 attackDir = (playerMovement != null && playerMovement.lastInputDirection.sqrMagnitude > 0.01f)
            ? playerMovement.lastInputDirection
            : transform.forward;

        Vector3 attackOrigin = transform.position + attackDir * attackRange;

        Collider[] hits = Physics.OverlapSphere(attackOrigin, attackRadius, enemyLayer);

        foreach (Collider hit in hits)
        {
            DetectionFSM enemy = hit.GetComponent<DetectionFSM>();
            if (enemy == null) continue;

            if (enemy.currentHealth <= 0 || enemy.currentState == DetectionFSM.EnemyState.Dead)
                hit.gameObject.SetActive(false);
        }
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