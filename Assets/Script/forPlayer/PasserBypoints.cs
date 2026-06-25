using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Place on any junction or waypoint GameObject.
/// Link neighbors manually in the Inspector to define valid path connections.
/// Requires a Trigger Collider on the same GameObject.
/// </summary>
public class PathNode : MonoBehaviour
{
    [Tooltip("Directly connected neighbor nodes along the path.")]
    public List<PathNode> neighbors = new List<PathNode>();

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.25f);

        // Draw lines to neighbors.
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.4f);
        foreach (PathNode n in neighbors)
            if (n != null)
                Gizmos.DrawLine(transform.position, n.transform.position);
    }
}