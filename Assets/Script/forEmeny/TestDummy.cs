using UnityEngine;
using UnityEngine.AI;

public class Pathogean : MonoBehaviour
{
    public Transform targetArea; // the area it should go to
    private NavMeshAgent agent;

    public int health = 10;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (targetArea != null)
            agent.SetDestination(targetArea.position);
    }

    void Update()
    {
        // Continuously move to target (in case it moves)
        if (targetArea != null)
        {
            agent.SetDestination(targetArea.position);
        }
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
            Die();
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (targetArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetArea.position, 0.5f);
        }
    }
}