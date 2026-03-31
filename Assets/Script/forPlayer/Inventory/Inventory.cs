using UnityEngine;

public class Inventory : MonoBehaviour
{
    public O2Item currentItem; // Only one slot

    public bool HasItem => currentItem != null;

    public void AddItem(O2Item item)
    {
        currentItem = item;
        Debug.Log("Picked up: " + item.itemName);
    }

    public void ClearItem()
    {
        currentItem = null;
    }
}