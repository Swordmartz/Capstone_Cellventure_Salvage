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

    private PlayerMovementTry playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementTry>();
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

        // Use last joystick direction, fallback to forward
        Vector3 shootDir = playerMovement.lastInputDirection.sqrMagnitude > 0.01f
            ? playerMovement.lastInputDirection
            : transform.forward;

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
}