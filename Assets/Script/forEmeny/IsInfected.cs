using UnityEngine;

public class IsInfected : MonoBehaviour
{
    [SerializeField] private bool isInfected;

    [Tooltip("Animator to update whenever infection state changes. If left empty, will try to auto-find one on this GameObject.")]
    [SerializeField] private Animator animator;

    [Tooltip("Name of the bool parameter on the Animator Controller to sync with isInfected.")]
    [SerializeField] private string animatorParam = "Infected";

    public bool Infected => isInfected;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void SetInfected(bool value)
    {
        isInfected = value;

        if (animator != null)
        {
            animator.SetBool(animatorParam, isInfected);
        }
        else
        {
            // Not necessarily an error -- some infected objects might not have
            // an Animator/animation for this state -- but worth a heads-up in
            // case it was expected to be wired up.
            Debug.LogWarning($"[IsInfected] {name}: SetInfected({value}) called but no Animator is assigned or found on this GameObject.");
        }
    }
}