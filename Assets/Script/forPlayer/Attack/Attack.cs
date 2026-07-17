using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    public int damage = 2;
    public float attackRange = 1.5f;
    public float attackRadius = 1f;
    public LayerMask enemyLayer;

    public float meleeCooldown = 1f;
    private float lastMeleeTime = -999f;

    public Animator anim;

    [Tooltip("Optional: wire up the ComboCounterUI to track melee hits.")]
    public ComboCounterUI comboCounter;

    private PlayerMovementTry playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementTry>();
        anim = GetComponent<Animator>();
    }

    public void PerformAttack()
    {
        Debug.Log("PerformAttack called");

        if (Time.time < lastMeleeTime + meleeCooldown)
        {
            Debug.Log("Melee attack on cooldown!");
            return;
        }

        anim.SetBool("IsAttacking", true);
        lastMeleeTime = Time.time;

        Vector3 attackDir = (playerMovement != null && playerMovement.lastInputDirection.sqrMagnitude > 0.01f)
            ? playerMovement.lastInputDirection.normalized
            : transform.forward;

        Vector3 attackOrigin = transform.position + attackDir * attackRange;

        Collider[] hits = Physics.OverlapSphere(attackOrigin, attackRadius, enemyLayer);
        Debug.Log($"OverlapSphere found {hits.Length} colliders on enemyLayer at {attackOrigin}");

        foreach (Collider hit in hits)
        {
            Debug.Log($"Checking collider: {hit.name} (layer: {LayerMask.LayerToName(hit.gameObject.layer)})");

            // Use GetComponentInParent in case the collider is on a child
            // object (hitbox/capsule) while DetectionFSM lives on the root.
            DetectionFSM enemy = hit.GetComponentInParent<DetectionFSM>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"Hit {enemy.name} with melee attack!");

                comboCounter?.RegisterExternalHit();
            }
            else
            {
                Debug.Log($"{hit.name} has no DetectionFSM in its hierarchy — check where the script lives.");
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
                ? playerMovement.lastInputDirection.normalized
                : transform.forward)
            : transform.forward;
        Gizmos.DrawWireSphere(transform.position + dir * attackRange, attackRadius);
    }
}