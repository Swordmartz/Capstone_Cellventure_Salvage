using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class O2Item : ScriptableObject
{
    public string itemName;
    [TextArea]
    public string description;
    public Sprite icon;
}