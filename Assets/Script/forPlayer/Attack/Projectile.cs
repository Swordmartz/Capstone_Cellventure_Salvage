using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;   // assign prefab in Inspector
    public Transform firePoint;           // empty GameObject at gun barrel
    public float projectileSpeed = 10f;
    public float projectileLifeTime = 5f;
    public int projectileDamage = 1;

    private PlayerMovementTry playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementTry>();
    }

    // Called by UI Button OnClick()
    public void Shoot()
    {
        // Use last joystick direction from PlayerMovementTry
        Vector3 shootDir = playerMovement.lastInputDirection;
        if (shootDir.sqrMagnitude < 0.01f)
        {
            // fallback: use player facing if no input
            shootDir = transform.forward;
        }

        // Spawn projectile at firePoint
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDir));

        // Add movement + collision behavior directly here
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = proj.AddComponent<Rigidbody>();
            rb.isKinematic = true; // move manually
        }

        proj.AddComponent<ProjectileBehaviour>().Init(projectileSpeed, projectileLifeTime, projectileDamage);
    }
}

// Inner class for projectile behavior
public class ProjectileBehaviour : MonoBehaviour
{
    private float speed;
    private float lifeTime;
    private int damage;

    public void Init(float spd, float life, int dmg)
    {
        speed = spd;
        lifeTime = life;
        damage = dmg;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        DetectionFSM enemy = other.GetComponent<DetectionFSM>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject); // only destroys projectile itself
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
