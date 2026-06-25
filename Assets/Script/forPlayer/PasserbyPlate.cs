using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Passerby Agent — always moves forward.
/// When an unvisited PathNode enters detection range, it groups
/// visible nodes into corridors and picks one at random.
/// Plain Transform movement, no NavMesh, no physics.
/// </summary>
public class PasserbyAgent : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Target")]
    public Transform mainTarget;

    [Header("Movement")]
    public float speed = 3f;
    public float rotationSpeed = 8f;

    [Header("Node Detection")]
    [Tooltip("Radius in which the agent notices PathNodes.")]
    public float detectionRange = 4f;

    [Tooltip("Layer mask the PathNode objects are on.")]
    public LayerMask nodeLayer;

    [Header("Corridor Grouping")]
    [Tooltip("Nodes within this angle of each other count as the same corridor.")]
    public float corridorAngleThreshold = 40f;

    [Header("Backtrack Prevention")]
    [Tooltip("Only applied after the first decision. Excludes nodes more than this many degrees behind the agent.")]
    [Range(0f, 180f)]
    public float backAngleTolerance = 120f;

    [Header("Arrival")]
    public float targetThreshold = 0.6f;

    [Header("Debug")]
    public bool drawGizmos = true;

    // ─── Runtime ──────────────────────────────────────────────────────────────

    Vector3 _moveDir;
    HashSet<PathNode> _visitedNodes = new HashSet<PathNode>();
    bool _hasDecidedOnce = false;   // backtrack filter only active after first pick
    bool _arrived;

    // Gizmos
    List<List<PathNode>> _lastCorridors = new List<List<PathNode>>();
    int _chosenCorridorIndex = -1;
    PathNode _lastDecisionNode;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (mainTarget == null)
        {
            Debug.LogError("[PasserbyAgent] No mainTarget assigned!");
            enabled = false;
            return;
        }

        _moveDir = GetFlatDir(transform.position, mainTarget.position);
        Debug.Log("[PasserbyAgent] Started — walking toward main target.");
    }

    // ─── Update ───────────────────────────────────────────────────────────────

    void Update()
    {
        if (_arrived) return;

        if (Vector3.Distance(transform.position, mainTarget.position) <= targetThreshold)
        {
            OnArrived();
            return;
        }

        // Check if a new unvisited node just entered range.
        PathNode nearNode = GetNearestUnvisitedNode();
        if (nearNode != null)
        {
            _visitedNodes.Add(nearNode);
            DecideAt(nearNode);
        }

        // Always keep moving.
        transform.position += _moveDir * speed * Time.deltaTime;

        if (_moveDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(_moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                                                     rotationSpeed * Time.deltaTime);
        }
    }

    // ─── Get Nearest Unvisited Node in Range ──────────────────────────────────

    PathNode GetNearestUnvisitedNode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, nodeLayer);
        PathNode nearest = null;
        float best = float.MaxValue;

        foreach (Collider col in hits)
        {
            PathNode node = col.GetComponent<PathNode>();
            if (node == null) continue;
            if (_visitedNodes.Contains(node)) continue;

            float d = Vector3.Distance(transform.position, node.transform.position);
            if (d < best) { best = d; nearest = node; }
        }

        return nearest;
    }

    // ─── Decide at a Node ─────────────────────────────────────────────────────

    void DecideAt(PathNode triggerNode)
    {
        _lastDecisionNode = triggerNode;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, nodeLayer);
        List<PathNode> candidates = new List<PathNode>();

        foreach (Collider col in hits)
        {
            PathNode n = col.GetComponent<PathNode>();
            if (n == null) continue;

            // Backtrack filter — only after first decision so we don't kill early candidates.
            if (_hasDecidedOnce)
            {
                Vector3 toNode = GetFlatDir(transform.position, n.transform.position);
                float dot = Vector3.Dot(toNode, _moveDir);

                // backAngleTolerance = 120 means exclude nodes more than 120° away from forward.
                // cos(120°) = -0.5, so dot < -0.5 = behind the agent.
                float cutoff = Mathf.Cos(backAngleTolerance * Mathf.Deg2Rad);
                if (dot < cutoff)
                {
                    Debug.Log($"[PasserbyAgent] '{n.gameObject.name}' excluded — behind agent (dot {dot:F2} < cutoff {cutoff:F2}).");
                    continue;
                }
            }

            candidates.Add(n);
        }

        List<List<PathNode>> corridors = GroupIntoCorridors(candidates);
        _lastCorridors = corridors;

        Debug.Log($"[PasserbyAgent] DecideAt '{triggerNode.gameObject.name}' — {candidates.Count} candidate(s), {corridors.Count} corridor(s).");

        if (corridors.Count == 0)
        {
            _moveDir = GetFlatDir(transform.position, mainTarget.position);
            _chosenCorridorIndex = -1;
            Debug.Log("[PasserbyAgent] No forward corridors — heading to main target.");
            return;
        }

        // Equal probability — one vote per corridor.
        int chosenIdx = Random.Range(0, corridors.Count);
        _chosenCorridorIndex = chosenIdx;
        List<PathNode> chosen = corridors[chosenIdx];

        PathNode target = GetClosestNode(chosen);
        _moveDir = GetFlatDir(transform.position, target.transform.position);
        _hasDecidedOnce = true;

        Debug.Log($"[PasserbyAgent] Chose corridor {chosenIdx + 1}/{corridors.Count} → steering toward '{target.gameObject.name}'.");
    }

    // ─── Corridor Grouping ────────────────────────────────────────────────────

    List<List<PathNode>> GroupIntoCorridors(List<PathNode> candidates)
    {
        List<List<PathNode>> corridors = new List<List<PathNode>>();

        foreach (PathNode node in candidates)
        {
            Vector3 toNode = node.transform.position - transform.position;
            float angle = Mathf.Atan2(toNode.x, toNode.z) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            bool merged = false;

            foreach (List<PathNode> corridor in corridors)
            {
                Vector3 toFirst = corridor[0].transform.position - transform.position;
                float firstAngle = Mathf.Atan2(toFirst.x, toFirst.z) * Mathf.Rad2Deg;
                if (firstAngle < 0f) firstAngle += 360f;

                if (Mathf.Abs(Mathf.DeltaAngle(angle, firstAngle)) <= corridorAngleThreshold)
                {
                    corridor.Add(node);
                    merged = true;
                    break;
                }
            }

            if (!merged)
                corridors.Add(new List<PathNode> { node });
        }

        return corridors;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    Vector3 GetFlatDir(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        dir.y = 0f;
        return dir.normalized;
    }

    PathNode GetClosestNode(List<PathNode> nodes)
    {
        PathNode closest = null;
        float best = float.MaxValue;

        foreach (PathNode n in nodes)
        {
            float d = Vector3.Distance(transform.position, n.transform.position);
            if (d < best) { best = d; closest = n; }
        }

        return closest;
    }

    void OnArrived()
    {
        _arrived = true;
        Debug.Log("[PasserbyAgent] Reached main target. Done.");
    }

    // ─── Gizmos ───────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.08f);
        Gizmos.DrawSphere(transform.position, detectionRange);
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (!Application.isPlaying) return;

        // Move direction arrow — white.
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, _moveDir * 2f);

        Color[] corridorColors =
        {
            new Color(0f,   1f,   1f),
            new Color(1f,   1f,   0f),
            new Color(1f,   0.5f, 0f),
            new Color(0.5f, 0f,   1f),
            new Color(0f,   1f,   0f),
            new Color(1f,   0f,   0.5f),
        };

        for (int c = 0; c < _lastCorridors.Count; c++)
        {
            Color col = corridorColors[c % corridorColors.Length];
            if (_chosenCorridorIndex >= 0 && c != _chosenCorridorIndex)
                col = new Color(col.r, col.g, col.b, 0.2f);

            Gizmos.color = col;
            foreach (PathNode n in _lastCorridors[c])
            {
                if (n == null) continue;
                Gizmos.DrawWireSphere(n.transform.position, 0.35f);
                Gizmos.DrawLine(transform.position, n.transform.position);
            }
        }

        if (_lastDecisionNode != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_lastDecisionNode.transform.position, 0.45f);
        }

        if (mainTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, mainTarget.position);
            Gizmos.DrawWireSphere(mainTarget.position, targetThreshold);
        }
    }
}