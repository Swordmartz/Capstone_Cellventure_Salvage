using System.Collections;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UI;

public class NeutrophilAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 2f;    
    public float attackRadius = 1f;   
    public int damage = 10;
    public LayerMask enemyLayer;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;   
    public float projectileSpeed = 10f;
    public float projectileLifetime = 3f;
    public int projectileDamage = 5;

    [Header("Joystick UI")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;

    [Header("Attack Visual")]
    public GameObject attackPoint;    

    [Header("Attack Timing")]
    public float attackVisibleTime = 0.2f;
    public float attackCooldown = 1f;

    private Vector3 lastDirection = Vector3.forward;
    private bool canAttack = true;

    void Start()
    {
        if (attackPoint != null)
            attackPoint.SetActive(false);
    }

    void Update()
    {
        Vector2 input = GetJoystickInput();
        if (input.magnitude > 0.1f)
        {
            lastDirection = new Vector3(input.x, 0, input.y).normalized;
        }
    }

    // Melee attack
    public void Attack()
    {
        if (!canAttack) return;
        canAttack = false;

        Vector3 attackPos = transform.position + lastDirection * attackRange;

        if (attackPoint != null)
        {
            attackPoint.transform.position = attackPos;
            attackPoint.SetActive(true);
            StartCoroutine(HideAttackPoint());
        }

        Collider[] hitEnemies = Physics.OverlapSphere(attackPos, attackRadius, enemyLayer);
        foreach (Collider enemy in hitEnemies)
        {
            enemy.GetComponent<Pathogen>()?.TakeDamage(damage);
        }

        StartCoroutine(AttackCooldownTimer());
    }

    // Projectile attack
    // Projectile attack
    public void ShootProjectile()
    {
        if (!canAttack) return;
        canAttack = false;

        if (projectilePrefab != null)
        {
            // Rotate projectile to face lastDirection
            Quaternion rotation = Quaternion.LookRotation(lastDirection, Vector3.up);
            GameObject proj = Instantiate(projectilePrefab, transform.position, rotation);

            // Assign damage + layer
            Projectile projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.damage = projectileDamage;
                projectile.enemyLayer = enemyLayer;
                projectile.speed = projectileSpeed;
                projectile.lifetime = projectileLifetime;
            }
        }

        StartCoroutine(AttackCooldownTimer());
    }



    private IEnumerator HideAttackPoint()
    {
        yield return new WaitForSeconds(attackVisibleTime);
        if (attackPoint != null)
            attackPoint.SetActive(false);
    }

    private IEnumerator AttackCooldownTimer()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private Vector2 GetJoystickInput()
    {
        if (joystickHandle == null || joystickBackground == null)
            return Vector2.zero; // joystick not available, no input

        Vector2 direction = joystickHandle.anchoredPosition / (joystickBackground.sizeDelta.x / 2f);
        return Vector2.ClampMagnitude(direction, 1f);
    }


    private void OnDrawGizmosSelected()
    {
        // Melee gizmo
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.transform.position, attackRadius);
        }

        // Projectile gizmo
        Gizmos.color = Color.blue;
        Vector3 start = transform.position;
        Vector3 end = start + lastDirection * projectileSpeed * projectileLifetime;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, 0.2f); // spawn point
        Gizmos.DrawWireSphere(end, 0.2f);   // expected end point
    }

}
