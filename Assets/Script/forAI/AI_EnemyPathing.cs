using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Pathogen : MonoBehaviour
{
    [Header("Pathing")]
    public Transform targetArea;            // original target area
    private Transform originalTarget;       // store original target
    private NavMeshAgent agent;

    [Header("Health")]
    public int health = 10;

    [Header("Hide Settings")]
    public LayerMask hideableLayer;         // objects to hide behind
    public float hideDuration = 3f;         // seconds to hide

    [Header("Player Detection")]
    public Transform player;                // assign player
    public float detectionRadius = 5f;      // player detection range

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        originalTarget = targetArea;

        if (targetArea != null)
            agent.SetDestination(targetArea.position);
    }

    

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
            gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (targetArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetArea.position, 0.5f);
        }

        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, detectionRadius);
        }
    }
}