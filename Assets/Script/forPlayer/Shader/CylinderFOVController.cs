using UnityEngine;

/// <summary>
/// CylinderFOVController
/// ─────────────────────
/// Attach to your PLAYER. Sends player position to the shader every
/// frame so the cylindrical reveal is always centered on the player.
///
/// The reveal is a vertical cylinder aligned to the world Y axis —
/// camera angle has no effect on the shape.
/// Use CylinderBottom / CylinderTop to control the height range.
/// </summary>
public class CylinderFOVController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Your cylinder / outer body mesh Renderer.")]
    public Renderer cylinder;

    [Header("Reveal Settings")]
    public float revealRadius = 3f;
    [Range(0f, 2f)]
    public float edgeSoftness = 0.5f;

    [Header("Height Range")]
    [Tooltip("World-unit offset from player Y where the hole starts. " +
             "0 = player's feet. Negative = below the player.")]
    public float cylinderBottom = 0f;

    [Tooltip("World-unit offset from player Y where the hole ends. " +
             "e.g. 4 = 4 units above the player.")]
    public float cylinderTop = 4f;

    private Material _mat;

    private static readonly int PropPlayerPos = Shader.PropertyToID("_PlayerPos");
    private static readonly int PropCameraPos = Shader.PropertyToID("_CameraPos");
    private static readonly int PropRevealRadius = Shader.PropertyToID("_RevealRadius");
    private static readonly int PropEdgeSoftness = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int PropCylinderBottom = Shader.PropertyToID("_CylinderBottom");
    private static readonly int PropCylinderTop = Shader.PropertyToID("_CylinderTop");

    void Start()
    {
        if (cylinder == null)
        {
            Debug.LogError("[CylinderFOV] Assign your Cylinder Renderer in the Inspector!");
            return;
        }
        _mat = cylinder.sharedMaterial;
        SyncStaticProps();
    }

    void Update()
    {
        if (_mat == null) return;

        // Player world position
        _mat.SetVector(PropPlayerPos, transform.position);

        // Camera position (kept for shader compatibility, no longer used for cut shape)
        if (Camera.main != null)
            _mat.SetVector(PropCameraPos, Camera.main.transform.position);

        // Sync all props every frame so Inspector tweaks apply live
        SyncStaticProps();
    }

    void SyncStaticProps()
    {
        if (_mat == null) return;
        _mat.SetFloat(PropRevealRadius, revealRadius);
        _mat.SetFloat(PropEdgeSoftness, edgeSoftness);
        _mat.SetFloat(PropCylinderBottom, cylinderBottom);
        _mat.SetFloat(PropCylinderTop, cylinderTop);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw the cylindrical reveal zone in the Scene view
        Vector3 playerPos = transform.position;
        float bottom = playerPos.y + cylinderBottom;
        float top = playerPos.y + cylinderTop;

        // Bottom circle
        DrawCircleGizmo(new Vector3(playerPos.x, bottom, playerPos.z),
                        revealRadius, new Color(0.2f, 1f, 0.5f, 0.8f));

        // Top circle
        DrawCircleGizmo(new Vector3(playerPos.x, top, playerPos.z),
                        revealRadius, new Color(0.2f, 1f, 0.5f, 0.8f));

        // Vertical edges
        Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.4f);
        int sides = 8;
        for (int i = 0; i < sides; i++)
        {
            float a = i * (360f / sides) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * revealRadius;
            Gizmos.DrawLine(new Vector3(playerPos.x, bottom, playerPos.z) + offset,
                            new Vector3(playerPos.x, top, playerPos.z) + offset);
        }

        // Labels
        UnityEditor.Handles.Label(
            new Vector3(playerPos.x + revealRadius + 0.1f, bottom, playerPos.z),
            $"Bottom ({cylinderBottom:+0.##;-0.##;0})");
        UnityEditor.Handles.Label(
            new Vector3(playerPos.x + revealRadius + 0.1f, top, playerPos.z),
            $"Top (+{cylinderTop:0.##})");
    }

    static void DrawCircleGizmo(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        int segments = 40;
        float step = 360f / segments * Mathf.Deg2Rad;
        for (int i = 0; i < segments; i++)
        {
            float a1 = i * step;
            float a2 = (i + 1) * step;
            Vector3 p1 = center + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * radius;
            Gizmos.DrawLine(p1, p2);
        }
    }
#endif
}