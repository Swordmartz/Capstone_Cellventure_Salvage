using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WallPush : MonoBehaviour
{
    NavMeshAgent agent;

    [SerializeField] float wallCheckDistance = 2f;
    [SerializeField] float pushStrength = 2f;
    [SerializeField] LayerMask wallMask;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (agent.isStopped) return;

        Vector3 pushDirection = Vector3.zero;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, wallCheckDistance, wallMask))
            {
                float strength = 1f - (hit.distance / wallCheckDistance);
                pushDirection -= dir * strength;
            }
        }

        if (pushDirection != Vector3.zero)
        {
            // Just move the transform directly, no warp no SetDestination
            transform.position += pushDirection.normalized * pushStrength * Time.deltaTime;
        }
    }
}