using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
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

    [Header("Infection Timer")]
    [Tooltip("How long (in seconds) RBC stays infected before spawning more malaria.")]
    [SerializeField] private float infectionDuration = 5f;

    [Tooltip("Malaria prefab(s) to spawn when the infection timer reaches 0. Must have a component that starts its own behavior on spawn (e.g. MalariaFSM).")]
    [SerializeField] private GameObject malariaSpawnPrefab;

    [Tooltip("Minimum number of malaria instances to spawn when the timer completes (inclusive).")]
    [SerializeField] private int minSpawnCount = 1;

    [Tooltip("Maximum number of malaria instances to spawn when the timer completes (inclusive).")]
    [SerializeField] private int maxSpawnCount = 5;

    [Tooltip("Random radius around this RBC's position that spawned malaria instances are scattered within.")]
    [SerializeField] private float spawnRadius = 0.5f;

    private float currentT;
    private SpriteRenderer spriteRenderer;

    // The "normal" sprite to show when no state-override applies — i.e. the
    // original sprite, or whatever a SpriteSwapZone last swapped in via SwapSprite().
    private Sprite baseSprite;

    // Used in Update() to detect manual Inspector toggles of these bools
    // (as opposed to changes made through SetInfected()/SetDeoxygenated()/SwapSprite()).
    private bool lastDeoxygenated;
    private bool lastIsInfected;

    // Counts down from infectionDuration while isInfected is true. When it
    // reaches 0, SpawnMalaria() fires once and the timer stops until the
    // next time this RBC becomes infected again.
    private float infectionTimer;
    private bool infectionTimerRunning;

    /// <summary>Whether this RBC is currently infected.</summary>
    public bool IsInfected => isInfected;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseSprite = spriteRenderer.sprite;
        lastDeoxygenated = deoxygenated;
        lastIsInfected = isInfected;

        RBCTracker.Instance?.RegisterRBC();
        if (isInfected)
            RBCTracker.Instance?.RegisterInfection();
    }

    private void OnDestroy()
    {
        RBCTracker.Instance?.RegisterRBCDeath(isInfected);
    }

    private void Update()
    {
        // Catch manual/Inspector toggles of deoxygenated/isInfected that didn't
        // go through the setter methods, and refresh the sprite accordingly.
        if (deoxygenated != lastDeoxygenated || isInfected != lastIsInfected)
        {
            // isInfected was toggled directly in the Inspector rather than via
            // SetInfected() -> keep the infection timer and RBCTracker in sync with it too.
            if (isInfected != lastIsInfected)
            {
                SetInfectionTimerActive(isInfected);
                ReportInfectionChange(isInfected);
            }

            RefreshSpriteState();
            lastDeoxygenated = deoxygenated;
            lastIsInfected = isInfected;
        }

        if (infectionTimerRunning)
        {
            infectionTimer -= Time.deltaTime;
            if (infectionTimer <= 0f)
            {
                infectionTimerRunning = false;
                SpawnMalaria();

                // RBC is consumed once it finishes spawning more malaria.
                Destroy(gameObject);
                return;
            }
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

        // Reached the end of the spline -> RBC exits the level, destroy it.
        if (currentT >= 1f)
        {
            Destroy(gameObject);
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
    /// which override sprite (if any) is shown — see RefreshSpriteState(). Also
    /// starts/stops the infection timer that spawns more malaria on completion.
    /// </summary>
    public void SetInfected(bool infected)
    {
        bool wasInfected = isInfected;

        isInfected = infected;
        lastIsInfected = infected;
        RefreshSpriteState();

        if (infected != wasInfected)
        {
            SetInfectionTimerActive(infected);
            ReportInfectionChange(infected);
        }
    }

    /// <summary>
    /// Tells RBCTracker about an infection state transition. Only call this
    /// when isInfected has actually flipped - calling it without a real
    /// change would over/under-count infectedRBC.
    /// </summary>
    private void ReportInfectionChange(bool nowInfected)
    {
        if (RBCTracker.Instance == null) return;

        if (nowInfected)
            RBCTracker.Instance.RegisterInfection();
        else
            RBCTracker.Instance.RegisterCure();
    }

    /// <summary>
    /// Starts (true) or stops (false) the infection countdown timer.
    /// </summary>
    private void SetInfectionTimerActive(bool active)
    {
        if (active)
        {
            infectionTimer = infectionDuration;
            infectionTimerRunning = true;
        }
        else
        {
            infectionTimerRunning = false;
        }
    }

    /// <summary>
    /// Called automatically when the infection timer reaches 0. Spawns
    /// spawnCount instances of malariaSpawnPrefab, scattered within spawnRadius
    /// of this RBC's current position.
    /// </summary>
    private void SpawnMalaria()
    {
        if (malariaSpawnPrefab == null) return;

        // UnityEngine.Random.Range's max is exclusive for ints, so +1 makes
        // maxSpawnCount itself a possible result (i.e. truly inclusive 1-5).
        int spawnCount = UnityEngine.Random.Range(minSpawnCount, maxSpawnCount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 offset = UnityEngine.Random.insideUnitSphere * spawnRadius;
            offset.y = 0f; // keep spawns on the same horizontal plane as the RBC
            Vector3 desiredPosition = transform.position + offset;

            // Snap onto the actual walkable NavMesh surface before spawning.
            // RBC follows a 3D spline, so its own position may not sit exactly
            // on the baked NavMesh -- spawning slightly off it is what causes
            // the new malaria's NavMeshAgent to jitter/float, since it fights
            // every frame trying to reconcile an off-mesh starting position.
            Vector3 spawnPosition = desiredPosition;
            bool onMesh = NavMesh.SamplePosition(desiredPosition, out NavMeshHit navHit, spawnRadius + 2f, NavMesh.AllAreas);
            if (onMesh)
            {
                spawnPosition = navHit.position;
            }

            GameObject instance = Instantiate(malariaSpawnPrefab, spawnPosition, Quaternion.identity);

            // Explicitly warp the agent onto the sampled point too, as a safety
            // net on top of the SamplePosition snap above -- Warp() correctly
            // places a NavMeshAgent without it trying to path/interpolate there.
            NavMeshAgent spawnedAgent = instance.GetComponent<NavMeshAgent>();
            if (spawnedAgent != null && onMesh)
            {
                spawnedAgent.Warp(spawnPosition);
            }
        }
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

        if (isInfected)
            ReportInfectionChange(false);

        isInfected = false;
        lastIsInfected = false;
        SetInfectionTimerActive(false);
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