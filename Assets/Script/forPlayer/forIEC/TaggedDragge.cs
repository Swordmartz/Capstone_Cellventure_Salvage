using UnityEngine;

/// <summary>
/// Attach this to any draggable object alongside DraggableObject.
/// Assign its FoodType in the inspector.
/// The DropZone reads this to know what value to apply to the HealthBar.
/// </summary>
public class TaggedDraggable : MonoBehaviour
{
    [SerializeField] private FoodType foodType = FoodType.None;

    public FoodType FoodType => foodType;
}