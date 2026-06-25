using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class PasserbySplinePathB : MonoBehaviour
{
    public enum PasserbyState
    {
        FollowingPath,
        DivertingToHeal,
        Healing
    }

    [Header("Spline")]
    public SplineContainer splineContainer;

    [Header("Movement")]
    public float speed = 1f;
    public bool loop = true;
    public bool destroyAtEnd = false;

    [Header("Sprite")]
    public SpriteRenderer spriteRenderer;
    public bool flipSpriteBasedOnDirection = true;

    // Pool callback — PasserbySpawnerB subscribes to this
    public System.Action OnPathFinished;

    // All currently-active instances of THIS class only. Kept separate from
    // PasserbySplinePath's own list, so this script's scenes never see or
    // affect passersby spawned by the original script, and vice versa.
    public static readonly List<PasserbySplinePathB> ActiveInstances = new List<PasserbySplinePathB>();

    /// <summary>True while this passerby is free to be diverted to a heal target.</summary>
    public bool IsAvailableForHeal => _state == PasserbyState.FollowingPath && enabled;

    private PasserbyState _state = PasserbyState.FollowingPath;

    private float _t = 0f;
    private Vector3 _lastPosition;

    // Heal-diversion state
    private Transform _healTarget;
    private float _healArriveDistance = 0.2f;
    private System.Action<PasserbySplinePathB> _onHealArrived;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();

        _lastPosition = transform.position;
    }

    void OnEnable()
    {
        if (!ActiveInstances.Contains(this))
            ActiveInstances.Add(this);
    }

    void OnDisable()
    {
        ActiveInstances.Remove(this);
    }

    void Start()
    {
        if (splineContainer != null)
            transform.position = EvaluateWorld(0f);

        _lastPosition = transform.position;
    }

    void Update()
    {
        switch (_state)
        {
            case PasserbyState.DivertingToHeal:
                UpdateDivertToHeal();
                break;

            case PasserbyState.Healing:
                // Stand still while the heal target is processing the heal.
                break;

            case PasserbyState.FollowingPath:
            default:
                UpdateFollowPath();
                break;
        }
    }

    void UpdateFollowPath()
    {
        if (splineContainer == null) return;

        float splineLength = splineContainer.CalculateLength();
        if (splineLength <= 0f) return;

        _t += (speed / splineLength) * Time.deltaTime;

        if (_t >= 1f)
        {
            if (loop)
            {
                _t -= 1f;
            }
            else
            {
                _t = 1f;
                transform.position = EvaluateWorld(1f);

                if (destroyAtEnd)
                {
                    // Notify spawner to return to pool instead of destroying
                    OnPathFinished?.Invoke();
                }
                else
                {
                    enabled = false;
                }
                return;
            }
        }

        Vector3 newPosition = EvaluateWorld(_t);
        Vector3 direction = newPosition - _lastPosition;

        transform.position = newPosition;
        FlipSprite(direction);
        _lastPosition = newPosition;
    }

    // ── Heal diversion ──────────────────────────────────────────────────────

    /// <summary>
    /// Makes this passerby ignore its spline path and walk straight toward
    /// the given target. Calls onArrived once it gets within arriveDistance.
    /// </summary>
    public void DivertToHeal(Transform target, float arriveDistance, System.Action<PasserbySplinePathB> onArrived)
    {
        if (target == null) return;

        _healTarget = target;
        _healArriveDistance = arriveDistance;
        _onHealArrived = onArrived;
        _state = PasserbyState.DivertingToHeal;
        enabled = true; // in case it had stopped at the end of a non-looping path
    }

    /// <summary>
    /// Sends the passerby back to following its spline from where it left off.
    /// Call this once healing is finished with the target.
    /// </summary>
    public void ResumePath()
    {
        _state = PasserbyState.FollowingPath;
        _healTarget = null;
        _onHealArrived = null;
        _lastPosition = transform.position;
        enabled = true;
    }

    void UpdateDivertToHeal()
    {
        if (_healTarget == null)
        {
            ResumePath();
            return;
        }

        Vector3 toTarget = _healTarget.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance <= _healArriveDistance)
        {
            _state = PasserbyState.Healing;
            _onHealArrived?.Invoke(this);
            return;
        }

        Vector3 direction = toTarget.normalized;
        Vector3 newPosition = transform.position + direction * speed * Time.deltaTime;

        transform.position = newPosition;
        FlipSprite(direction);
        _lastPosition = newPosition;
    }

    // ── Pool support ─────────────────────────────────────────────────────────

    /// <summary>Resets the passerby back to the start of the spline for pool reuse.</summary>
    public void ResetPath()
    {
        _t = 0f;
        enabled = true;
        _state = PasserbyState.FollowingPath;
        _healTarget = null;
        _onHealArrived = null;

        if (splineContainer != null)
        {
            transform.position = EvaluateWorld(0f);
            _lastPosition = transform.position;
        }
    }

    void FlipSprite(Vector3 direction)
    {
        if (!flipSpriteBasedOnDirection || spriteRenderer == null) return;

        float absX = Mathf.Abs(direction.x);
        float absZ = Mathf.Abs(direction.z);

        if (absX >= absZ)
        {
            if (direction.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (direction.x < -0.01f)
                spriteRenderer.flipX = true;
        }
        else
        {
            if (direction.z > 0.01f)
                spriteRenderer.flipX = false;
            else if (direction.z < -0.01f)
                spriteRenderer.flipX = true;
        }
    }

    public void SetStartT(float startT)
    {
        _t = startT % 1f;
    }

    Vector3 EvaluateWorld(float t)
    {
        Vector3 localPos = SplineUtility.EvaluatePosition(splineContainer.Spline, t);
        return splineContainer.transform.TransformPoint(localPos);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (splineContainer == null) return;

        int steps = 60;
        Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.8f);
        Vector3 prev = EvaluateWorld(0f);
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 next = EvaluateWorld(t);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        if (Application.isPlaying)
        {
            Vector3 pos = EvaluateWorld(_t);
            Vector3 fwd = EvaluateWorld(Mathf.Min(_t + 0.02f, 1f)) - pos;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(pos, fwd.normalized * 0.5f);
        }
    }
#endif
}