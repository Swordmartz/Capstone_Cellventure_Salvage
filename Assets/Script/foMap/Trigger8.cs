using UnityEngine;

public class Trigger8 : MonoBehaviour
{
    public AIforDialogue MAI;

    [Header("Item Check")]
    public Inventory playerInventory;
    public O2Item requiredItem;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            if (playerInventory == null || requiredItem == null)
            {
                Debug.LogWarning("Inventory or requiredItem not assigned!");
                return;
            }

            if (playerInventory.HasItem && playerInventory.currentItem == requiredItem)
            {
                triggered = true;
                StartCoroutine(MAI.DialogueSequenceIRB8());
            }
            else
            {
                // Wrong item or no item
                Debug.Log("Wrong item or no item! Playing wrong dialogue.");
                // TODO: StartCoroutine(MAI.WrongItemDialogue());
            }
        }
    }
}