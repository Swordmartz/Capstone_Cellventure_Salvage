using UnityEngine;

public class ItemSpriteSwapper : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;
    public SpriteRenderer targetRenderer;

    [Header("Item Requirement")]
    public O2Item requiredItem;

    [Header("Sprite to Show")]
    public Sprite newSprite;

    private Sprite originalSprite;
    private bool spriteSwapped = false;

    void Start()
    {
        if (targetRenderer != null)
            originalSprite = targetRenderer.sprite;
    }

    void Update()
    {
        if (inventory == null || targetRenderer == null || requiredItem == null) return;

        bool hasRequiredItem = inventory.currentItem == requiredItem;

        if (hasRequiredItem && !spriteSwapped)
        {
            targetRenderer.sprite = newSprite;
            spriteSwapped = true;
        }
        else if (!hasRequiredItem && spriteSwapped)
        {
            targetRenderer.sprite = originalSprite;
            spriteSwapped = false;
        }
    }
}