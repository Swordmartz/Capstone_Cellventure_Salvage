using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;   // assign prefab in Inspector
    public Transform firePoint;           // empty GameObject at gun barrel
    public float projectileSpeed = 10f;
    public float projectileLifeTime = 5f;
    public int projectileDamage = 1;

    [Header("Cooldown Settings")]
    public float shootCooldown = 0.5f;
    private float lastShootTime = -999f;

    // Lets other scripts (e.g. NeutrophilNPCAlly's attack-type selection)
    // check cooldown state before deciding to call Shoot(), instead of
    // calling it and having it silently no-op.
    public bool IsOnCooldown => Time.time < lastShootTime + shootCooldown;

    [Tooltip("Optional. If assigned, used as a facing fallback when the joystick is neutral - reads " +
             "the same LastMoveX/LastMoveY floats MeleeAttack uses, since transform.forward isn't " +
             "driven by this project's facing system (a billboard script owns transform.rotation " +
             "instead). If left empty, the script will try GetComponent<Animator>() at Start.")]
    public Animator anim;

    private static readonly int LastMoveXHash = Animator.StringToHash("LastMoveX");
    private static readonly int LastMoveYHash = Animator.StringToHash("LastMoveY");

    private PlayerMovementTry playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementTry>();
        if (anim == null)
            anim = GetComponent<Animator>();
    }

    // Called by UI Button OnClick()
    public void Shoot()
    {
        if (Time.time < lastShootTime + shootCooldown)
        {
            Debug.Log("Projectile attack on cooldown!");
            return;
        }
        lastShootTime = Time.time;

        Vector3 shootDir = GetShootDirection();

        // Spawn projectile
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDir));

        // Initialize behaviour
        ProjectileBehaviour behaviour = proj.GetComponent<ProjectileBehaviour>();
        if (behaviour == null)
        {
            behaviour = proj.AddComponent<ProjectileBehaviour>();
        }
        behaviour.Init(projectileSpeed, projectileLifeTime, projectileDamage, 15f, 5f);
    }

    // Priority order, matching MeleeAttack.GetAttackDirection():
    //   1. Actively pushed joystick direction (lastInputDirection).
    //   2. Otherwise, wherever the character is currently facing, read
    //      from the Animator's LastMoveX/LastMoveY floats - these persist
    //      the last non-zero facing even while idle/neutral-stick, so a
    //      shot fired without stick input still aims where the character
    //      is visually looking rather than an arbitrary/stale rotation.
    //   3. transform.forward as a last resort if there's no Animator or
    //      the facing floats haven't been set to anything yet.
    private Vector3 GetShootDirection()
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
                // Same X-inverted, Y-as-is conversion MeleeAttack uses, for
                // consistency between the two attack types.
                return new Vector3(-facing.x, 0f, facing.y).normalized;
            }
        }

        return transform.forward;
    }
}