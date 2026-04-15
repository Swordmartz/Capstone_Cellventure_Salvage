using UnityEngine;

public class Trigger5 : MonoBehaviour
{
    public AIforDialogue MAI; // Assign AIGuide
    public Inventory playerInventory; // Assign player's inventory

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            Inventory playerInventory = other.GetComponentInParent<Inventory>();
            if (playerInventory != null && playerInventory.HasItem)
            {
                triggered = true;
                StartCoroutine(MAI.DialogueSequenceIRB5());
            }
            else
            {
                Debug.Log("Inventory null: " + (playerInventory == null) + " | HasItem: " + (playerInventory != null && playerInventory.HasItem));
            }
        }
    }
}