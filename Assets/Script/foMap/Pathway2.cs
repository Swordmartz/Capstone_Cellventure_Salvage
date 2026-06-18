using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class PasserbySplinePath : MonoBehaviour
{
    [Header("Spline")]
    public SplineContainer splineContainer;

    [Header("Movement")]
    public float speed = 1f;
    public bool loop = true;
    public bool destroyAtEnd = false;

    [Header("Sprite")]
    public SpriteRenderer spriteRenderer;
    public bool flipSpriteBasedOnDirection = true;

    // Pool callback — PasserbySpawner subscribes to this
    public System.Action OnPathFinished;

    private float _t = 0f;
    private Vector3 _lastPosition;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();

        _lastPosition = transform.position;
    }

    void Start()
    {
        if (splineContainer != null)
            transform.position = EvaluateWorld(0f);

        _lastPosition = transform.position;
    }

    void Update()
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

    /// <summary>Resets the passerby back to the start of the spline for pool reuse.</summary>
    public void ResetPath()
    {
        _t = 0f;
        enabled = true;

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