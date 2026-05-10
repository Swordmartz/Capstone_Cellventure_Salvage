using UnityEngine;

/// <summary>
/// Attach to your existing Box Collider trigger GameObject.
/// When a tagged draggable is dropped inside:
///   1. Reads its FoodType
///   2. Applies the corresponding value to the HealthBar
///   3. Disables the GameObject
/// </summary>
public class DropZone : MonoBehaviour
{
    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[DropZone] {name}: Collider was not a trigger — fixed automatically.");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        DraggableObject draggable = other.GetComponent<DraggableObject>();
        if (draggable == null) return;

        // Only trigger when object has just been dropped (not while being dragged)
        if (draggable.IsDragging()) return;

        TaggedDraggable tagged = other.GetComponent<TaggedDraggable>();

        if (tagged == null)
        {
            Debug.LogWarning($"[DropZone] {other.name} has no TaggedDraggable component — no value applied.");
        }
        else
        {
            // Apply value to the health bar
            if (HealthBar.Instance != null)
                HealthBar.Instance.ApplyValue(tagged.FoodType);
            else
                Debug.LogWarning("[DropZone] No HealthBar instance found in scene!");
        }

        // Disable the object regardless of whether it has a tag
        Debug.Log($"[DropZone] {other.name} ({(tagged != null ? tagged.FoodType.ToString() : "untagged")}) dropped — disabling.");
        other.gameObject.SetActive(false);
    }
}