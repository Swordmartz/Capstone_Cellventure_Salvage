using UnityEngine;

public class PlayerAnimatorHandler : MonoBehaviour
{
    public Inventory playerInventory;

    [Header("Animator References")]
    public Animator animator;
    public RuntimeAnimatorController defaultController;
    public RuntimeAnimatorController itemController;

    private void Start()
    {
        // Ensure default at start
        if (animator != null && defaultController != null)
        {
            animator.runtimeAnimatorController = defaultController;
        }
    }

    private void Update()
    {
        if (playerInventory == null || animator == null) return;

        // If player has item → use item animator
        if (playerInventory.HasItem)
        {
            if (animator.runtimeAnimatorController != itemController)
            {
                animator.runtimeAnimatorController = itemController;
            }
        }
        else
        {
            // No item → revert to default
            if (animator.runtimeAnimatorController != defaultController)
            {
                animator.runtimeAnimatorController = defaultController;
            }
        }
    }
}