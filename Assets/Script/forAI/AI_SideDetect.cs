using UnityEngine;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class WallAvoidance : MonoBehaviour
{
    public float checkDistance = 0.5f;   // how far to raycast to detect walls
    public float pushStrength = 0.05f;   // how much to nudge away

    void Update()
    {
        RaycastHit hit;

        // Check right side
        if (Physics.Raycast(transform.position, transform.right, out hit, checkDistance))
        {
            transform.position += -transform.right * pushStrength;
        }
        // Check left side
        else if (Physics.Raycast(transform.position, -transform.right, out hit, checkDistance))
        {
            transform.position += transform.right * pushStrength;
        }
    }
}
