using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns a collectible near a target player's CURRENT position on a repeating
/// interval - not near this object's own position, and not near the
/// camera. No longer trigger-based: this doesn't need a Collider on it at
/// all. Every interval it raycasts straight down (world -Y) from the
/// target player's own position, and if that ray hits spawnableLayer,
/// spawns the collectible right there (with a small amount of validated
/// jitter). Trigger colliders (e.g. a billboard/sprite quad) are ignored
/// by the raycast. NavMesh is not used.
///
/// The target player can be assigned explicitly in the Inspector (or via
/// SetTargetPlayer at runtime). If left unassigned, it falls back to
/// GameObject.FindGameObjectWithTag(playerTag) every interval, same as before.
/// </summary>
public class CollectibleSpawnPoint : MonoBehaviour
{
    [Header("Target Player")]
    [Tooltip("Optional explicit reference to the player this spawner should follow. If set, " +
             "this is used directly and playerTag/FindGameObjectWithTag is skipped entirely. " +
             "Leave empty to fall back to tag-based lookup below.")]
    [SerializeField] private Transform targetPlayer;

    [Tooltip("Used to find the player via GameObject.FindGameObjectWithTag every interval, " +
             "but ONLY if targetPlayer above is not assigned.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Collectible prefab to spawn near the player.")]
    [SerializeField] private GameObject collectiblePrefab;

    [Header("Spawn Interval")]
    [Tooltip("Seconds between spawn attempts. First spawn attempt happens after one interval " +
             "has elapsed, not immediately on scene start.")]
    [SerializeField] private float spawnInterval = 5f;

    [Tooltip("If true, this spawner stops permanently after its first SUCCESSFUL spawn " +
             "(i.e. the gate was met and a collectible was actually spawned). If false, it " +
             "keeps spawning one collectible every interval indefinitely, as long as the " +
             "gate keeps being met each time.")]
    [SerializeField] private bool oneShot = false;

    [Tooltip("The spawner starts running as soon as this object is enabled. Uncheck this if " +
             "you want to start/stop it manually from another script via StartSpawning()/StopSpawning().")]
    [SerializeField] private bool runOnEnable = true;

    [Header("Spawn Placement")]
    [Tooltip("Horizontal distance, along the player's facing direction (flattened onto the XZ plane), " +
             "to offset the down-raycast from the player's own position. This is what makes the " +
             "collectible spawn out in front of the player instead of right at their feet.")]
    [SerializeField] private float forwardDistance = 3f;

    [Tooltip("Height above that forward-offset point to start the down-raycast from. Needs to be tall " +
             "enough to clear any terrain bumps, railings, etc. at the offset point so the raycast " +
             "starts above them rather than inside/under them.")]
    [SerializeField] private float forwardRaycastHeight = 5f;

    [Tooltip("How far straight down the raycast travels (starting from forwardRaycastHeight above the " +
             "forward-offset point) to check for spawnableLayer. Must be at least forwardRaycastHeight " +
             "plus however far below that point the real floor can be.")]
    [SerializeField] private float raycastDistance = 50f;

    [Tooltip("After the main down-raycast hits spawnableLayer, the collectible spawns at a " +
             "random point within this radius of that HIT point (not the player) - each candidate is " +
             "re-validated with its own straight-down raycast, so it always stays on the mesh.")]
    [SerializeField] private float spawnJitterRadius = 1f;

    [Tooltip("Vertical offset applied to the final spawn point, e.g. to sit the collectible slightly " +
             "above the ground instead of exactly on the surface.")]
    [SerializeField] private float spawnHeightOffset = 0.5f;

    [Tooltip("Only a collider on this layer counts as valid ground to spawn on.")]
    [SerializeField] private LayerMask spawnableLayer;

    private enum InfectionComparison
    {
        AtOrBelow,
        AtOrAbove
    }

    [Header("Infection Bar Gate")]
    [Tooltip("If true, each interval only spawns once RBCTracker.Instance.InfectionPercentage " +
             "meets requiredInfectionPercentage (per infectionComparison below). If RBCTracker isn't " +
             "found in the scene, the gate is treated as not met and nothing spawns that interval.")]
    [SerializeField] private bool gateOnInfectionPercentage = true;

    [Tooltip("AtOrBelow: spawn once infection has dropped to/under this level (e.g. reward for " +
             "curing most of the infection). AtOrAbove: spawn once infection has risen to/over this " +
             "level (e.g. warning pickup for a worsening infection).")]
    [SerializeField] private InfectionComparison infectionComparison = InfectionComparison.AtOrBelow;

    [Range(0f, 1f)]
    [Tooltip("The InfectionPercentage level (0-1) required before a spawn attempt will spawn anything.")]
    [SerializeField] private float requiredInfectionPercentage = 0.5f;

    [Header("Debug")]
    [Tooltip("Draws a LIVE straight-down raycast from the player every frame in the Scene view (green = " +
             "hit spawnableLayer, red = missed, magenta = hit something but wrong layer), plus the last " +
             "actual spawn attempt's raycasts. Trigger colliders (e.g. a billboard/sprite quad) are " +
             "ignored so they can't block or clutter this readout.")]
    [SerializeField] private bool drawDebugGizmos = true;

    private bool hasFired;
    private float timer;
    private bool isRunning;

    // --- Gizmo debug state, updated each time SpawnNearPlayer runs ---
    private Vector3 lastPlayerPos;
    private readonly List<RayAttemptDebug> lastRayAttempts = new List<RayAttemptDebug>();

    private struct RayAttemptDebug
    {
        public Vector3 origin;
        public Vector3 end;
        public bool hit;
        public Vector3 hitPoint;
    }

    private void OnEnable()
    {
        timer = 0f;
        if (runOnEnable)
            StartSpawning();
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    /// <summary>Begins the interval timer. Safe to call even if already running.</summary>
    public void StartSpawning()
    {
        isRunning = true;
    }

    /// <summary>Stops the interval timer without resetting hasFired/progress.</summary>
    public void StopSpawning()
    {
        isRunning = false;
    }

    /// <summary>
    /// Assigns (or clears, if passed null) the explicit player this spawner should follow.
    /// While assigned, playerTag/FindGameObjectWithTag is skipped entirely. Passing null
    /// reverts to tag-based lookup on the next spawn attempt.
    /// </summary>
    public void SetTargetPlayer(Transform player)
    {
        targetPlayer = player;
    }

    private void Update()
    {
        if (!isRunning)
            return;

        if (hasFired && oneShot)
        {
            isRunning = false;
            return;
        }

        timer += Time.deltaTime;
        if (timer < spawnInterval)
            return;

        timer = 0f;
        TrySpawn();
    }

    // Resolves which player transform to use this attempt: the explicitly
    // assigned targetPlayer if set, otherwise falls back to a tag lookup.
    private Transform ResolvePlayer()
    {
        if (targetPlayer != null)
            return targetPlayer;

        GameObject found = GameObject.FindGameObjectWithTag(playerTag);
        return found != null ? found.transform : null;
    }

    private void TrySpawn()
    {
        if (collectiblePrefab == null)
        {
            Debug.LogWarning($"[CollectibleSpawnPoint] {name}: spawn interval elapsed but no collectiblePrefab is assigned.");
            return;
        }

        Transform player = ResolvePlayer();
        if (player == null)
        {
            if (targetPlayer == null)
            {
                Debug.LogWarning($"[CollectibleSpawnPoint] {name}: spawn interval elapsed but no GameObject tagged " +
                    $"'{playerTag}' was found in the scene, and no targetPlayer is assigned.");
            }
            return;
        }

        if (!IsInfectionGateMet())
            return;

        SpawnNearPlayer(player);

        if (oneShot)
            hasFired = true;
    }

    // Checks RBCTracker.Instance.InfectionPercentage against
    // requiredInfectionPercentage using infectionComparison. Returns true
    // immediately (gate not enforced) if gateOnInfectionPercentage is off.
    // If the gate IS enabled but RBCTracker can't be found, this returns
    // false rather than spawning unconditionally - fail closed, not open.
    private bool IsInfectionGateMet()
    {
        if (!gateOnInfectionPercentage)
            return true;

        var tracker = RBCTracker.Instance;
        if (tracker == null)
        {
            Debug.LogWarning($"[CollectibleSpawnPoint] {name}: gateOnInfectionPercentage is enabled but " +
                "no RBCTracker.Instance was found - treating the gate as not met.");
            return false;
        }

        bool met = infectionComparison == InfectionComparison.AtOrBelow
            ? tracker.InfectionPercentage <= requiredInfectionPercentage
            : tracker.InfectionPercentage >= requiredInfectionPercentage;

        Debug.Log($"[CollectibleSpawnPoint] {name}: gate check - current InfectionPercentage=" +
            $"{tracker.InfectionPercentage:P1}, required={requiredInfectionPercentage:P1}, " +
            $"comparison={infectionComparison}, met={met}.");

        return met;
    }

    // Computes the player's facing direction flattened onto the XZ plane
    // (so pitch/tilt doesn't push the spawn point up into the air or down
    // into the floor). Falls back to world forward if the player is
    // looking straight up/down and the flattened vector would be ~zero.
    private Vector3 GetFlatForward(Transform player)
    {
        Vector3 flat = player.forward;
        flat.y = 0f;
        return flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
    }

    // Finds the point out in front of the player (forwardDistance along
    // their flattened facing direction), then raycasts straight down
    // (world -Y) from forwardRaycastHeight above that point. Trigger
    // colliders (e.g. a billboard/sprite quad) are ignored so they can't
    // block or falsely satisfy this. If that ray hits spawnableLayer, the
    // collectible spawns right there - with a small amount of random
    // jitter around the hit point so it's not always the exact same spot,
    // where each jittered candidate is independently re-validated with its
    // OWN straight-down raycast before being accepted, so it always ends
    // up on that same mesh.
    private void SpawnNearPlayer(Transform player)
    {
        lastPlayerPos = player.position;
        lastRayAttempts.Clear();

        Vector3 forwardPoint = player.position + GetFlatForward(player) * forwardDistance;
        Vector3 mainOrigin = forwardPoint + Vector3.up * forwardRaycastHeight;
        bool mainHit = Physics.Raycast(mainOrigin, Vector3.down, out RaycastHit mainHitInfo,
            raycastDistance, spawnableLayer, QueryTriggerInteraction.Ignore);

        lastRayAttempts.Add(new RayAttemptDebug
        {
            origin = mainOrigin,
            end = mainOrigin + Vector3.down * raycastDistance,
            hit = mainHit,
            hitPoint = mainHit ? mainHitInfo.point : mainOrigin + Vector3.down * raycastDistance
        });

        if (!mainHit)
        {
            Debug.LogWarning($"[CollectibleSpawnPoint] {name}: down-raycast from in front of the player found " +
                $"no spawnableLayer within {raycastDistance} units - skipped spawning this interval. " +
                "Increase raycastDistance (or forwardRaycastHeight) if the floor is further below that point than that.");
            return;
        }

        // Try the exact hit point first, then a few small jittered offsets
        // around it, each independently re-validated on the mesh.
        const int maxAttempts = 6;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 candidate;
            if (attempt == 0)
            {
                candidate = mainHitInfo.point;
            }
            else
            {
                Vector2 jitter = Random.insideUnitCircle * spawnJitterRadius;
                candidate = mainHitInfo.point + new Vector3(jitter.x, 0f, jitter.y);
            }

            Vector3 candidateOrigin = candidate + Vector3.up * 2f;
            bool candidateHit = Physics.Raycast(candidateOrigin, Vector3.down, out RaycastHit candidateHitInfo,
                raycastDistance + 2f, spawnableLayer, QueryTriggerInteraction.Ignore);

            lastRayAttempts.Add(new RayAttemptDebug
            {
                origin = candidateOrigin,
                end = candidateOrigin + Vector3.down * (raycastDistance + 2f),
                hit = candidateHit,
                hitPoint = candidateHit ? candidateHitInfo.point : candidateOrigin
            });

            if (candidateHit)
            {
                Vector3 spawnPos = candidateHitInfo.point + Vector3.up * spawnHeightOffset;
                Instantiate(collectiblePrefab, spawnPos, Quaternion.identity);
                return;
            }
        }

        Debug.LogWarning($"[CollectibleSpawnPoint] {name}: found spawnableLayer directly below the player " +
            "but couldn't land a jittered point on it after several attempts - skipped spawning this interval.");
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos)
            return;

        // --- LIVE straight-down ray from the player, updated every frame ---
        // Ignores trigger colliders entirely (QueryTriggerInteraction.Ignore)
        // so a billboard/sprite quad's trigger collider can't block this or
        // get reported as a false hit - it goes straight down through it to
        // whatever real (non-trigger) geometry is actually beneath the player.
        Transform livePlayer = Application.isPlaying ? ResolvePlayer() : targetPlayer;

        if (livePlayer != null)
        {
            Vector3 forwardPoint = livePlayer.position + GetFlatForward(livePlayer) * forwardDistance;
            Vector3 origin = forwardPoint + Vector3.up * forwardRaycastHeight;
            Vector3 end = origin + Vector3.down * raycastDistance;

            bool liveHitLayer = Physics.Raycast(origin, Vector3.down, out RaycastHit liveHit,
                raycastDistance, spawnableLayer, QueryTriggerInteraction.Ignore);

            bool liveHitAnything = Physics.Raycast(origin, Vector3.down, out RaycastHit liveHitAny,
                raycastDistance, ~0, QueryTriggerInteraction.Ignore);

            Gizmos.color = liveHitLayer ? Color.green : Color.red;
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawSphere(origin, 0.2f);

            if (liveHitLayer)
            {
                Gizmos.DrawSphere(liveHit.point, 0.35f);
            }
            else if (liveHitAnything)
            {
                // Something was hit, just not on spawnableLayer - mark it
                // distinctly (magenta) so it's obvious this is a layer
                // mismatch rather than nothing being there at all.
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(liveHitAny.point, 0.3f);
            }
        }

        // --- Snapshot from the last actual spawn attempt ---
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        foreach (var attempt in lastRayAttempts)
        {
            Gizmos.color = attempt.hit ? Color.green : Color.red;
            Gizmos.DrawLine(attempt.origin, attempt.end);
            Gizmos.DrawSphere(attempt.origin, 0.15f);
            if (attempt.hit)
                Gizmos.DrawSphere(attempt.hitPoint, 0.25f);
        }
    }
}