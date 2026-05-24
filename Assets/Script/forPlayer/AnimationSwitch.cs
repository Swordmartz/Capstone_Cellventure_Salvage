using UnityEngine;

public class PlayerAnimatorHandler : MonoBehaviour
{
    public Inventory playerInventory;

    [Header("Animator References")]
    public Animator animator;
    public RuntimeAnimatorController defaultController;
    public RuntimeAnimatorController itemController;

    [Header("Trigger Item")]
    public O2Item requiredItem; // Only this item triggers the change

    [Header("Oxygen Effect")]
    [SerializeField] private CellOxygenEffect oxygenEffect;

    private void Start()
    {
        if (animator != null && defaultController != null)
            animator.runtimeAnimatorController = defaultController;

        if (oxygenEffect == null)
            oxygenEffect = GetComponent<CellOxygenEffect>();
    }

    private void Update()
    {
        if (playerInventory == null || animator == null) return;

        // Only trigger if the specific required item is in inventory
        bool hasRequiredItem = playerInventory.HasItem &&
                               playerInventory.currentItem == requiredItem;

        if (hasRequiredItem)
        {
            if (animator.runtimeAnimatorController != itemController)
            {
                animator.runtimeAnimatorController = itemController;

                if (oxygenEffect != null)
                    oxygenEffect.PlayOxygenated();

                Debug.Log("[PlayerAnimatorHandler] Switched to oxygenated.");
            }
        }
        else
        {
            if (animator.runtimeAnimatorController != defaultController)
            {
                animator.runtimeAnimatorController = defaultController;

                if (oxygenEffect != null)
                    oxygenEffect.PlayDeoxygenated();

                Debug.Log("[PlayerAnimatorHandler] Switched to deoxygenated.");
            }
        }
    }
}