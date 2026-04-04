using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Pathogen : MonoBehaviour
{
    public int health = 10;
    public float moveSpeed = 5f;
    public float acceleration = 8f; // default acceleration
    public bool isSlowed = false; // add this at the top

    private float originalSpeed;
    private float originalAcceleration;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.acceleration = acceleration;

            originalSpeed = agent.speed;
            originalAcceleration = agent.acceleration;
        }
        else
        {
            originalSpeed = moveSpeed;
            originalAcceleration = acceleration;
        }
    }

    public void TakeDamage(int amount)
    {
        // If invincible, ignore damage from projectiles
        DetectionTest detection = GetComponent<DetectionTest>();
        if (detection != null && detection.isInvincible)
        {
            Debug.Log($"{name} is invincible! Ignoring projectile damage.");
            return;
        }

        // Apply damage normally
        health -= amount;
        health = Mathf.Max(health, 0); // clamp to 0

        if (health <= 0)
            gameObject.SetActive(false);
    }


    public void ApplySlow(float slowFactor, float duration)
    {
        // If invincible, ignore slow
        DetectionTest detection = GetComponent<DetectionTest>();
        if (detection != null && detection.isInvincible)
        {
            Debug.Log($"{name} is invincible! Slow effect ignored.");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(SlowCoroutine(slowFactor, duration));
    }


    private IEnumerator SlowCoroutine(float slowFactor, float duration)
    {
        isSlowed = true;

        if (agent != null)
        {
            agent.speed = originalSpeed * slowFactor;
            agent.acceleration = originalAcceleration * (1f / slowFactor);
        }

        yield return new WaitForSeconds(duration);

        if (agent != null)
        {
            agent.speed = originalSpeed;
            agent.acceleration = originalAcceleration;
        }

        isSlowed = false; // slow finished
    }
}
