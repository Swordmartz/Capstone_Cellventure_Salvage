using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class MultiTouchDragManager : MonoBehaviour
{
    [Header("Global Settings")]
    [SerializeField] private float dragSmoothness = 0.1f;
    [SerializeField] private LayerMask draggableLayer;

    private Camera mainCamera;
    private readonly Dictionary<int, DragData> activeDrags = new Dictionary<int, DragData>();
    private readonly HashSet<int> liveTouchIds = new HashSet<int>();
    private readonly List<int> toRemove = new List<int>();

    private class DragData
    {
        public DraggableObject DraggedObject;
        public Vector3 Offset;
        public float WorldZ;
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogError("[DragManager] No MainCamera found!");
        else
            Debug.Log($"[DragManager] Camera found: {mainCamera.name} at pos {mainCamera.transform.position}, orthographic: {mainCamera.orthographic}");
    }

    private void Update()
    {
        HandleMultiTouch();

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#endif
    }

    private void HandleMultiTouch()
    {
        liveTouchIds.Clear();

        foreach (var touch in Touch.activeTouches)
        {
            int id = touch.touchId;
            liveTouchIds.Add(id);

            switch (touch.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    TryStartDrag(id, touch.screenPosition);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    UpdateDrag(id, touch.screenPosition);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    EndDrag(id);
                    break;
            }
        }

        toRemove.Clear();
        foreach (int id in activeDrags.Keys)
            if (!liveTouchIds.Contains(id))
                toRemove.Add(id);
        foreach (int id in toRemove)
            EndDrag(id);
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    private bool wasMouseDown = false;

    private void HandleMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 pos = mouse.position.ReadValue();
        bool isDown = mouse.leftButton.isPressed;

        if (isDown && !wasMouseDown)
        {
            TryStartDrag(-1, pos);
        }
        else if (isDown && wasMouseDown)
        {
            UpdateDrag(-1, pos);
        }
        else if (!isDown && wasMouseDown)
        {
            EndDrag(-1);
        }

        wasMouseDown = isDown;
    }
#endif

    private void TryStartDrag(int touchId, Vector2 screenPos)
    {
        if (activeDrags.ContainsKey(touchId)) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, draggableLayer))
            return;

        DraggableObject draggable = hit.collider.GetComponent<DraggableObject>();
        if (draggable == null || !draggable.CanDrag()) return;

        float worldZ = draggable.transform.position.z;

        Vector3? touchWorld = ProjectToWorldZ(screenPos, worldZ);
        if (!touchWorld.HasValue)
        {
            Debug.LogWarning("[DragManager] ProjectToWorldZ returned null — drag aborted.");
            return;
        }

        Vector3 offset = draggable.transform.position - touchWorld.Value;
        Debug.Log($"[DragManager] Drag started on {draggable.name} | worldZ:{worldZ} | touchWorld:{touchWorld.Value} | offset:{offset}");

        activeDrags[touchId] = new DragData
        {
            DraggedObject = draggable,
            Offset = offset,
            WorldZ = worldZ
        };

        draggable.OnDragStart();
    }

    private void UpdateDrag(int touchId, Vector2 screenPos)
    {
        if (!activeDrags.TryGetValue(touchId, out DragData data)) return;
        if (data.DraggedObject == null) { EndDrag(touchId); return; }

        Vector3? worldPos = ProjectToWorldZ(screenPos, data.WorldZ);
        if (!worldPos.HasValue)
        {
            Debug.LogWarning("[DragManager] ProjectToWorldZ returned null in UpdateDrag.");
            return;
        }

        Vector3 target = worldPos.Value + data.Offset;
        Debug.Log($"[DragManager] UpdateDrag | projected:{worldPos.Value} | target:{target}");
        data.DraggedObject.UpdateDragPosition(target, dragSmoothness);
    }

    private void EndDrag(int touchId)
    {
        if (!activeDrags.TryGetValue(touchId, out DragData data)) return;
        data.DraggedObject?.OnDragEnd();
        activeDrags.Remove(touchId);
        Debug.Log($"[DragManager] Drag ended for touchId:{touchId}");
    }

    private Vector3? ProjectToWorldZ(Vector2 screenPos, float worldZ)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        float dz = ray.direction.z;

        if (Mathf.Abs(dz) < 1e-6f)
        {
            Debug.LogWarning($"[DragManager] Ray direction Z is nearly zero ({dz}) — cannot project.");
            return null;
        }

        float t = (worldZ - ray.origin.z) / dz;

        if (t < 0f)
        {
            Debug.LogWarning($"[DragManager] Projection is behind camera (t={t}). worldZ:{worldZ} rayOrigin.z:{ray.origin.z}");
            return null;
        }

        return ray.GetPoint(t);
    }

    public int ActiveDragCount => activeDrags.Count;
}