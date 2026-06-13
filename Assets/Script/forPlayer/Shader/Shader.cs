using UnityEngine;

/// <summary>
/// CylinderSeeThroughSetup
/// ───────────────────────
/// Attach to your tube/pipe GameObject (single mesh, no caps).
///
/// SETUP (3 steps):
///   1. Put CylinderSeeThrough.shader in Assets/Shaders/
///   2. Create a Material with shader "Custom/CylinderSeeThrough"
///   3. Attach this script to your tube GameObject and assign the material
///
/// The cylindrical reveal is driven by CylinderFOVController on your Player.
/// Set CylinderBottom = 0 so the hole never cuts below the player's feet.
/// Set CylinderTop to however tall you want the reveal (e.g. 4 = 4 units up).
/// </summary>
[RequireComponent(typeof(Renderer))]
public class CylinderSeeThroughSetup : MonoBehaviour
{
    [Header("Required")]
    [Tooltip("Material using Custom/CylinderSeeThrough shader.")]
    public Material seeThroughMaterial;

    [Header("Camera Orbit Preview (optional)")]
    [Tooltip("Auto-orbit the camera around the tube to preview the effect.")]
    public bool orbitCamera = false;
    public float orbitSpeed = 25f;
    public float orbitDistance = 6f;
    public float orbitHeight = 2f;

    private Renderer _rend;
    private float _angle;

    void Start()
    {
        _rend = GetComponent<Renderer>();

        // Try assigning material from Inspector slot
        if (seeThroughMaterial != null)
        {
            _rend.material = seeThroughMaterial;
            return;
        }

        // Fallback: find shader by name and create material at runtime
        Shader s = Shader.Find("Custom/CylinderSeeThrough");
        if (s != null)
        {
            _rend.material = new Material(s);
            Debug.Log("[CylinderSeeThrough] Material created from shader at runtime.");
        }
        else
        {
            Debug.LogError("[CylinderSeeThrough] Shader 'Custom/CylinderSeeThrough' not found. " +
                           "Make sure CylinderSeeThrough.shader is in your Assets/Shaders/ folder.");
        }
    }

    void Update()
    {
        if (orbitCamera && Camera.main != null)
        {
            _angle += orbitSpeed * Time.deltaTime;
            float rad = _angle * Mathf.Deg2Rad;
            Camera.main.transform.position = transform.position
                + new Vector3(Mathf.Sin(rad) * orbitDistance,
                              orbitHeight,
                              Mathf.Cos(rad) * orbitDistance);
            Camera.main.transform.LookAt(transform.position);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (Camera.main == null) return;
        Vector3 toCamera = (Camera.main.transform.position - transform.position).normalized;

        // Arrow pointing toward camera
        Gizmos.color = new Color(0.3f, 1f, 1f, 0.8f);
        Gizmos.DrawRay(transform.position, toCamera * 2.5f);
        Gizmos.DrawSphere(transform.position + toCamera * 2.5f, 0.12f);

        // Arrow pointing away from camera
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
        Gizmos.DrawRay(transform.position, -toCamera * 2.5f);
        Gizmos.DrawSphere(transform.position + (-toCamera) * 2.5f, 0.12f);

        UnityEditor.Handles.Label(transform.position + toCamera * 2.8f, "👁 camera side");
        UnityEditor.Handles.Label(transform.position + (-toCamera) * 2.8f, "✓ far wall");
    }
#endif
}