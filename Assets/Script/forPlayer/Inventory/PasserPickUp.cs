using UnityEngine;

public class PasserbyItemPickup : MonoBehaviour
{
    [Header("Required Item")]
    public O2Item requiredItem;

    [Header("Sprites")]
    public Sprite beforePickupSprite;
    public Sprite afterPickupSprite;

    [Header("References")]
    public Inventory passerbyInventory;

    private SpriteRenderer spriteRenderer;
    public bool hasPickedUp = false;
    public GameObject pickedUpObject;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (beforePickupSprite != null)
            spriteRenderer.sprite = beforePickupSprite;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasPickedUp) return;

        PickupButton pickupButton = other.GetComponent<PickupButton>();

        if (pickupButton == null) return;
        if (pickupButton.itemToPickup != requiredItem) return;

        ReceiveItem(pickupButton.itemToPickup, other.gameObject);
    }

    public void ReceiveItem(O2Item item, GameObject itemObject)
    {
        if (hasPickedUp) return;

        if (passerbyInventory != null)
            passerbyInventory.AddItem(item);

        hasPickedUp = true;
        pickedUpObject = itemObject;

        if (afterPickupSprite != null)
            spriteRenderer.sprite = afterPickupSprite;
    }

    public void ResetPickup()
    {
        hasPickedUp = false;

        if (pickedUpObject != null)
        {
            pickedUpObject.SetActive(true);
            pickedUpObject = null;
        }

        if (beforePickupSprite != null)
            spriteRenderer.sprite = beforePickupSprite;
    }
}