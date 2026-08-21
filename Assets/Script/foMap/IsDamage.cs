using UnityEngine;

public class IsDamage : MonoBehaviour
{
    [Header("Damage State")]
    public bool isDamage = false;

    [Header("Count Settings")]
    public int currentCount = 0;
    public int maxCount = 5;

    [Header("Animator")]
    [Tooltip("Animator to update whenever isDamage changes. If left empty, will try to auto-find one on this GameObject.")]
    [SerializeField] private Animator animator;

    [Tooltip("Name of the bool parameter on the Animator Controller to sync with isDamage.")]
    [SerializeField] private string animatorParam = "Damage";

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    // Call this from an enemy's FSM when it starts staying at this object
    public void IncreaseCount()
    {
        currentCount++;
        currentCount = Mathf.Clamp(currentCount, 0, maxCount);

        if (currentCount >= maxCount)
        {
            SetDamage(true);
        }
    }

    // Call this from an enemy's FSM when it stops staying at this object
    public void DecreaseCount()
    {
        currentCount--;
        currentCount = Mathf.Clamp(currentCount, 0, maxCount);

        if (currentCount < maxCount)
        {
            SetDamage(false);
        }
    }

    // Centralized setter so isDamage and the Animator param can never drift
    // out of sync -- IncreaseCount/DecreaseCount both route through this
    // instead of assigning isDamage directly.
    private void SetDamage(bool value)
    {
        isDamage = value;

        if (animator != null)
        {
            animator.SetBool(animatorParam, isDamage);
        }
        else
        {
            Debug.LogWarning($"[IsDamage] {name}: SetDamage({value}) called but no Animator is assigned or found on this GameObject.");
        }
    }
}