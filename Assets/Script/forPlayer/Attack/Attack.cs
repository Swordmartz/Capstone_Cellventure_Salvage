using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    public int damage = 2;
    public float attackRange = 1.5f;
    public float attackRadius = 1f;
    public LayerMask enemyLayer;

    public float meleeCooldown = 1f; // seconds between attacks
    private float lastMeleeTime = -999f;

    public Animator anim;

    private PlayerMovementTry playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementTry>();
        anim = GetComponent<Animator>();
    }

    public void PerformAttack()
    {
        if (Time.time < lastMeleeTime + meleeCooldown)
        {
            Debug.Log("Melee attack on cooldown!");
            return;
        }

        anim.SetBool("IsAttacking", true);

        lastMeleeTime = Time.time;

        Vector3 attackDir = (playerMovement != null && playerMovement.lastInputDirection.sqrMagnitude > 0.01f)
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

    public void FinishAttack()
    {
        anim.SetBool("IsAttacking", false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 dir = Application.isPlaying
            ? (playerMovement?.lastInputDirection.sqrMagnitude > 0.01f == true
                ? playerMovement.lastInputDirection
                : transform.forward)
            : transform.forward;
        Gizmos.DrawWireSphere(transform.position + dir * attackRange, attackRadius);
    }
}
