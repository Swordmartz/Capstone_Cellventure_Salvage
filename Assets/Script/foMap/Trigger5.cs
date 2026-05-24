using UnityEngine;

public class Trigger5 : MonoBehaviour
{
    public AIforDialogue MAI;
    public Inventory playerInventory;
    public O2Item requiredItem;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            Inventory inv = other.GetComponentInParent<Inventory>();

            if (inv == null)
            {
                Debug.LogWarning("No Inventory found on Player!");
                return;
            }

            if (!inv.HasItem)
            {
                Debug.Log("Player has no item.");
                return;
            }

            triggered = true;

            if (inv.currentItem == requiredItem)
            {
                triggered = true;
                StartCoroutine(MAI.DialogueSequenceIRB5());
            }
            else
            {
                // Wrong item
                Debug.Log("Wrong item: " + inv.currentItem.itemName + " — Playing wrong dialogue.");
                // TODO: StartCoroutine(MAI.WrongItemDialogue());
            }
        }
    }
}