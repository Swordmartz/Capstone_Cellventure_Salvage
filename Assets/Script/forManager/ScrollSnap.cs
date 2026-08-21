using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// SETUP:
/// 1. Attach this script to the SAME GameObject as your ScrollRect
///    (the one with the ScrollRect component and a Mask/Viewport).
/// 2. Your level items should be children of ScrollRect.Content,
///    laid out with a Horizontal/Vertical Layout Group (or manually).
/// 3. Drag each level item (in the order you want them) into the
///    "Snap Items" list in the inspector. If you leave the list empty,
///    it will automatically use every child of Content instead.
/// 4. Set "Direction" to match your scroll axis, and make sure the
///    ScrollRect itself only has Horizontal OR Vertical checked
///    (whichever matches), not both.
/// 5. Subscribe to OnLevelCentered if you want to know which level
///    is currently centered (e.g. to highlight it / enable a Play button):
///       scrollSnap.OnLevelCentered += (index) => { ... };
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class ScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public enum Direction { Horizontal, Vertical }

    [Header("Setup")]
    [Tooltip("Axis your ScrollRect scrolls along.")]
    public Direction direction = Direction.Horizontal;

    [Tooltip("Assign the level items that should be snappable, in order. Leave empty to auto-use all children of Content instead.")]
    public List<RectTransform> snapItems = new List<RectTransform>();

    [Header("Snap Behaviour")]
    [Tooltip("How fast the content animates into the snapped position.")]
    public float snapSpeed = 12f;

    [Tooltip("Automatically snap once the scroll's momentum settles (not just after a drag).")]
    public bool snapOnMomentumStop = true;

    [Tooltip("Velocity (units/sec) below which the scroll is considered 'stopped'.")]
    public float velocityThreshold = 20f;

    [Tooltip("Snap to the first/center item as soon as the screen opens.")]
    public bool snapOnStart = true;

    [Header("Editor Gizmo")]
    [Tooltip("Draw a line in the Scene view showing where items snap to.")]
    public bool showSnapGizmo = true;

    [Tooltip("Color of the snap line gizmo.")]
    public Color gizmoColor = new Color(1f, 0f, 0f, 0.85f);

    /// <summary>Index (within Content) of the level currently centered on screen.</summary>
    public int CurrentIndex { get; private set; }

    /// <summary>Fired whenever a snap finishes, with the index of the centered item.</summary>
    public System.Action<int> OnLevelCentered;

    private ScrollRect _scrollRect;
    private RectTransform _content;
    private RectTransform _viewport;
    private bool _isDragging;
    private bool _isSnapping;
    private bool _wasMoving;

    private void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
        _content = _scrollRect.content;
        _viewport = _scrollRect.viewport != null ? _scrollRect.viewport : (RectTransform)_scrollRect.transform;
    }

    private IEnumerator Start()
    {
        if (snapOnStart)
        {
            // Wait a frame so layout groups finish positioning children first.
            yield return null;
            // Always start centered on the first item, regardless of scroll position.
            SnapToIndex(0, instant: true);
        }
    }

    /// <summary>Returns the assigned snap items, or falls back to Content's children if none were assigned.</summary>
    private List<RectTransform> GetItems()
    {
        if (snapItems != null && snapItems.Count > 0)
            return snapItems;

        List<RectTransform> children = new List<RectTransform>(_content.childCount);
        for (int i = 0; i < _content.childCount; i++)
        {
            RectTransform child = _content.GetChild(i) as RectTransform;
            if (child != null) children.Add(child);
        }
        return children;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _isSnapping = false;
        StopAllCoroutines();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        SnapToNearest();
    }

    private void Update()
    {
        if (!snapOnMomentumStop || _isDragging || _isSnapping) return;

        bool isMoving = _scrollRect.velocity.sqrMagnitude > velocityThreshold * velocityThreshold;

        // Trigger exactly once, on the moving -> stopped transition
        // (covers mouse-wheel scroll and inertia after a fast drag).
        if (_wasMoving && !isMoving)
        {
            SnapToNearest();
        }
        _wasMoving = isMoving;
    }

    /// <summary>Finds the item closest to the viewport center and animates it there. Call manually (e.g. from a "next/prev" button) if you like.</summary>
    public void SnapToNearest(bool instant = false)
    {
        List<RectTransform> items = GetItems();
        if (items.Count == 0) return;

        int nearestIndex = 0;
        float nearestDist = float.MaxValue;
        Vector2 viewportCenter = GetWorldCenter(_viewport);

        for (int i = 0; i < items.Count; i++)
        {
            RectTransform item = items[i];
            if (item == null || !item.gameObject.activeInHierarchy) continue;

            Vector2 itemCenter = GetWorldCenter(item);
            float dist = direction == Direction.Horizontal
                ? Mathf.Abs(itemCenter.x - viewportCenter.x)
                : Mathf.Abs(itemCenter.y - viewportCenter.y);

            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestIndex = i;
            }
        }

        SnapToIndex(nearestIndex, instant);
    }

    /// <summary>Jump to a specific index in the item list (e.g. tapping a level thumbnail directly).</summary>
    public void SnapToIndex(int index, bool instant = false)
    {
        List<RectTransform> items = GetItems();
        if (index < 0 || index >= items.Count) return;

        CurrentIndex = index;
        RectTransform target = items[index];

        StopAllCoroutines();
        if (instant)
        {
            _content.anchoredPosition = GetSnappedContentPosition(target);
            OnLevelCentered?.Invoke(CurrentIndex);
        }
        else
        {
            StartCoroutine(SnapRoutine(target));
        }
    }

    private IEnumerator SnapRoutine(RectTransform target)
    {
        _isSnapping = true;
        _scrollRect.velocity = Vector2.zero;

        Vector2 targetPos = GetSnappedContentPosition(target);

        while (Vector2.Distance(_content.anchoredPosition, targetPos) > 0.5f)
        {
            _content.anchoredPosition = Vector2.Lerp(_content.anchoredPosition, targetPos, Time.unscaledDeltaTime * snapSpeed);
            yield return null;
        }

        _content.anchoredPosition = targetPos;
        _isSnapping = false;
        OnLevelCentered?.Invoke(CurrentIndex);
    }

    /// <summary>
    /// Computes what content.anchoredPosition should be so that "target"
    /// ends up centered in the viewport, on the chosen axis only.
    /// </summary>
    private Vector2 GetSnappedContentPosition(RectTransform target)
    {
        Vector3 targetWorldCenter = GetWorldCenter(target);
        Vector2 targetLocalInViewport = _viewport.InverseTransformPoint(targetWorldCenter);
        Vector2 viewportLocalCenter = _viewport.rect.center;

        Vector2 delta = viewportLocalCenter - targetLocalInViewport;
        Vector2 result = _content.anchoredPosition;

        if (direction == Direction.Horizontal)
            result.x += delta.x;
        else
            result.y += delta.y;

        return result;
    }

    private static Vector2 GetWorldCenter(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }

    /// <summary>Gets the viewport rect even in edit mode, when Awake() hasn't run yet.</summary>
    private RectTransform GetViewportSafe()
    {
        if (_viewport != null) return _viewport;

        ScrollRect sr = GetComponent<ScrollRect>();
        if (sr == null) return null;

        return sr.viewport != null ? sr.viewport : (RectTransform)sr.transform;
    }

    private void OnDrawGizmos()
    {
        if (!showSnapGizmo) return;

        RectTransform viewport = GetViewportSafe();
        if (viewport == null) return;

        // corners: 0 = bottom-left, 1 = top-left, 2 = top-right, 3 = bottom-right
        Vector3[] corners = new Vector3[4];
        viewport.GetWorldCorners(corners);

        Gizmos.color = gizmoColor;

        if (direction == Direction.Horizontal)
        {
            // Vertical line through the horizontal center, marking where items land.
            Vector3 top = (corners[1] + corners[2]) * 0.5f;
            Vector3 bottom = (corners[0] + corners[3]) * 0.5f;
            Gizmos.DrawLine(bottom, top);
        }
        else
        {
            // Horizontal line through the vertical center.
            Vector3 left = (corners[0] + corners[1]) * 0.5f;
            Vector3 right = (corners[2] + corners[3]) * 0.5f;
            Gizmos.DrawLine(left, right);
        }
    }
}