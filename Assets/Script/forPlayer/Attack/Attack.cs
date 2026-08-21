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

    // Same convention as the Animator floats driven elsewhere (e.g. the
    // NPC ally's facing logic) - LastMoveX/LastMoveY hold the direction the
    // player was last actually facing, and keep that value even while
    // standing still. Reading them here lets an attack thrown with a
    // neutral stick still aim at whatever the player is visually facing,
    // instead of falling straight back to transform.forward (which may not
    // match the sprite/rig's facing at all in a top-down setup).
    private static readonly int LastMoveXHash = Animator.StringToHash("LastMoveX");
    private static readonly int LastMoveYHash = Animator.StringToHash("LastMoveY");

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

        Vector3 attackDir = GetAttackDirection();

        Vector3 attackOrigin = transform.position + attackDir * attackRange;

        Collider[] hits = Physics.OverlapSphere(attackOrigin, attackRadius, enemyLayer);
        Debug.Log($"OverlapSphere found {hits.Length} colliders on enemyLayer at {attackOrigin}");

        foreach (Collider hit in hits)
        {
            Debug.Log($"Checking collider: {hit.name} (layer: {LayerMask.LayerToName(hit.gameObject.layer)})");

            bool hitSomething = false;

            // Use GetComponentInParent in case the collider is on a child
            // object (hitbox/capsule) while the enemy script lives on the root.
            DetectionFSM detectionEnemy = hit.GetComponentInParent<DetectionFSM>();
            if (detectionEnemy != null)
            {
                detectionEnemy.TakeDamage(damage);
                Debug.Log($"Hit {detectionEnemy.name} with melee attack!");
                hitSomething = true;
            }

            InfluenzaFSM influenzaEnemy = hit.GetComponentInParent<InfluenzaFSM>();
            if (influenzaEnemy != null)
            {
                influenzaEnemy.TakeDamage(damage);
                Debug.Log($"Hit {influenzaEnemy.name} with melee attack!");
                hitSomething = true;
            }

            pneumonococcalFSM pneumonococcalEnemy = hit.GetComponentInParent<pneumonococcalFSM>();
            if (pneumonococcalEnemy != null)
            {
                pneumonococcalEnemy.TakeDamage(damage);
                Debug.Log($"Hit {pneumonococcalEnemy.name} with melee attack!");
                hitSomething = true;
            }

            EnemySplineFollower dengueEnemy = hit.GetComponentInParent<EnemySplineFollower>();
            if (dengueEnemy != null)
            {
                dengueEnemy.TakeDamage(damage);
                Debug.Log($"Hit {dengueEnemy.name} with melee attack!");
                hitSomething = true;
            }

            MalariaFSM malariaEnemy = hit.GetComponentInParent<MalariaFSM>();
            if (malariaEnemy != null)
            {
                malariaEnemy.TakeDamage(damage);
                Debug.Log($"Hit {malariaEnemy.name} with melee attack!");
                hitSomething = true;
            }

            if (hitSomething)
            {
                comboCounter?.RegisterExternalHit();
            }
            else
            {
                Debug.Log($"{hit.name} has no DetectionFSM, InfluenzaFSM, pneumonococcalFSM, EnemySplineFollower, or MalariaFSM in its hierarchy — check where the script lives.");
            }
        }
    }

    // Priority order:
    //   1. Actively pushed joystick direction (lastInputDirection) - the
    //      player is telling us exactly where to swing right now.
    //   2. Otherwise, wherever the player is currently facing, read from
    //      the Animator's LastMoveX/LastMoveY floats - these persist the
    //      last non-zero facing even while idle, so an attack thrown from
    //      a standstill still aims where the character is visually looking.
    //   3. transform.forward as a last resort, if there's no Animator or
    //      the facing floats haven't been set to anything yet.
    private Vector3 GetAttackDirection()
    {
        if (playerMovement != null && playerMovement.lastInputDirection.sqrMagnitude > 0.01f)
        {
            return playerMovement.lastInputDirection.normalized;
        }

        if (anim != null)
        {
            Vector2 facing = new Vector2(anim.GetFloat(LastMoveXHash), anim.GetFloat(LastMoveYHash));
            if (facing.sqrMagnitude > 0.01f)
            {
                // X is inverted here to match the ally's facing convention;
                // Y (world Z) stays as-is.
                return new Vector3(-facing.x, 0f, facing.y).normalized;
            }
        }

        return transform.forward;
    }

    public void FinishAttack()
    {
        anim.SetBool("IsAttacking", false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 dir = Application.isPlaying
            ? GetAttackDirection()
            : transform.forward;
        Gizmos.DrawWireSphere(transform.position + dir * attackRange, attackRadius);
    }
}