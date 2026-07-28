using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Moves this object (RBC) along a Spline in 3D space. The spline it follows is
/// assigned dynamically (typically by SplineSpawner right after Instantiate) via
/// AssignSpline() — RBC does not need a spline pre-set in the Inspector.
///
/// Sprite switching is handled separately by SpriteSwapZone triggers placed along
/// the path (see SpriteSwapZone.cs), so it works regardless of each spline's
/// length, knot count, or size.
///
/// Requires the Unity "Splines" package (com.unity.splines).
/// Attach to the RBC prefab alongside a SpriteRenderer and a 3D Collider
/// (or Rigidbody, if you want trigger events to be received reliably).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class RBCSplineSpriteSwitcher : MonoBehaviour
{
    [Header("Spline")]
    [Tooltip("Usually left empty and assigned at runtime via AssignSpline() by SplineSpawner. Can also be set manually for splines placed directly in the scene.")]
    [SerializeField] private SplineContainer splineContainer;

    [Tooltip("Index of the spline within the container (containers can hold multiple splines).")]
    [SerializeField] private int splineIndex = 0;

    [Tooltip("How fast RBC moves along the spline, in units of t (0-1) per second.")]
    [SerializeField] private float speed = 0.1f;

    [Tooltip("If true, RBC also rotates to face the spline's forward direction.")]
    [SerializeField] private bool alignToSplineDirection = true;

    [Tooltip("World-space up vector used when aligning to the spline direction.")]
    [SerializeField] private Vector3 upVector = Vector3.up;

    [Header("State")]
    [Tooltip("Set to true automatically whenever RBC's sprite is switched by a SpriteSwapZone.")]
    [SerializeField] private bool deoxygenated;

    [Tooltip("Whether RBC is currently infected.")]
    [SerializeField] private bool isInfected;

    [Tooltip("Sprite RBC switches to when infected (and NOT deoxygenated).")]
    [SerializeField] private Sprite infectedSprite;

    [Tooltip("Sprite RBC switches to when BOTH infected AND deoxygenated are true. Takes priority over infectedSprite.")]
    [SerializeField] private Sprite deoxygenatedSprite;

    private float currentT;
    private SpriteRenderer spriteRenderer;

    // The "normal" sprite to show when no state-override applies — i.e. the
    // original sprite, or whatever a SpriteSwapZone last swapped in via SwapSprite().
    private Sprite baseSprite;

    // Used in Update() to detect manual Inspector toggles of these bools
    // (as opposed to changes made through SetInfected()/SetDeoxygenated()/SwapSprite()).
    private bool lastDeoxygenated;
    private bool lastIsInfected;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseSprite = spriteRenderer.sprite;
        lastDeoxygenated = deoxygenated;
        lastIsInfected = isInfected;
    }

    private void Update()
    {
        // Catch manual/Inspector toggles of deoxygenated/isInfected that didn't
        // go through the setter methods, and refresh the sprite accordingly.
        if (deoxygenated != lastDeoxygenated || isInfected != lastIsInfected)
        {
            RefreshSpriteState();
            lastDeoxygenated = deoxygenated;
            lastIsInfected = isInfected;
        }

        if (!HasValidSpline()) return;

        // Advance progress along the spline
        currentT += speed * Time.deltaTime;
        currentT = Mathf.Clamp01(currentT);

        Spline spline = splineContainer.Splines[splineIndex];
        spline.Evaluate(currentT, out float3 position, out float3 tangent, out _);
        transform.position = splineContainer.transform.TransformPoint(position);

        if (alignToSplineDirection && math.lengthsq(tangent) > 0.0001f)
        {
            Vector3 worldTangent = splineContainer.transform.TransformDirection(tangent);
            if (worldTangent != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(worldTangent, upVector);
            }
        }
    }

    /// <summary>
    /// Assigns which spline this RBC should follow. Called by SplineSpawner right
    /// after spawning, so RBC doesn't need a spline hardcoded in the Inspector.
    /// </summary>
    public void AssignSpline(SplineContainer container, int index = 0, bool resetProgress = true)
    {
        splineContainer = container;
        splineIndex = index;

        if (resetProgress)
        {
            currentT = 0f;
            SetDeoxygenated(false);
        }
    }

    /// <summary>
    /// Called by a SpriteSwapZone when RBC enters it. Sets the new base sprite and
    /// marks RBC as deoxygenated. If RBC is also infected, deoxygenatedSprite
    /// (rather than this sprite) will be displayed, per RefreshSpriteState().
    /// </summary>
    public void SwapSprite(Sprite newSprite)
    {
        if (newSprite == null) return;
        baseSprite = newSprite;
        deoxygenated = true;
        lastDeoxygenated = true;
        RefreshSpriteState();
    }

    /// <summary>
    /// Sets whether RBC is deoxygenated (without changing the base sprite).
    /// </summary>
    public void SetDeoxygenated(bool value)
    {
        deoxygenated = value;
        lastDeoxygenated = value;
        RefreshSpriteState();
    }

    /// <summary>
    /// Sets whether RBC is infected. Combined with `deoxygenated`, this determines
    /// which override sprite (if any) is shown — see RefreshSpriteState().
    /// </summary>
    public void SetInfected(bool infected)
    {
        isInfected = infected;
        lastIsInfected = infected;
        RefreshSpriteState();
    }

    /// <summary>
    /// Central place that decides which sprite should be visible based on the
    /// current isInfected / deoxygenated combination:
    ///   infected + deoxygenated -> deoxygenatedSprite
    ///   infected only           -> infectedSprite
    ///   otherwise                -> baseSprite (original, or last SwapSprite result)
    /// </summary>
    private void RefreshSpriteState()
    {
        if (isInfected && deoxygenated && deoxygenatedSprite != null)
        {
            spriteRenderer.sprite = deoxygenatedSprite;
        }
        else if (isInfected && infectedSprite != null)
        {
            spriteRenderer.sprite = infectedSprite;
        }
        else
        {
            spriteRenderer.sprite = baseSprite;
        }
    }

    /// <summary>Resets RBC back to the start of its currently assigned spline.</summary>
    public void ResetToStart()
    {
        currentT = 0f;
        isInfected = false;
        lastIsInfected = false;
        SetDeoxygenated(false); // also calls RefreshSpriteState()
    }

    private bool HasValidSpline()
    {
        return splineContainer != null &&
               splineContainer.Splines != null &&
               splineIndex >= 0 &&
               splineIndex < splineContainer.Splines.Count;
    }
}