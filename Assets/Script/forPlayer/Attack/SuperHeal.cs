using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks a "super heal" stack. Once the stack is full, a button is shown.
/// Clicking it (via UseSuper) fully heals every HealTargetB found on the
/// Healable layer, then resets the stack.
/// </summary>
public class SuperHealAbility : MonoBehaviour
{
    [Header("Stack Settings")]
    [Tooltip("How much stack is needed before the super becomes usable.")]
    public int maxStack = 10;
    [SerializeField] private int currentStack = 0;

    [Header("UI")]
    [Tooltip("The super button GameObject. Should start INACTIVE in the scene — " +
             "this script activates it once the stack is full.")]
    public GameObject superButton;

    [Header("Heal Targeting")]
    [Tooltip("Set this to whatever layer your healable targets are on.")]
    public LayerMask healableLayer;
    [Tooltip("If true, heals every HealTargetB in the whole scene regardless of distance. " +
             "If false, only heals ones within healRadius of this object.")]
    public bool healEntireScene = true;
    [Tooltip("Only used when healEntireScene is false.")]
    public float healRadius = 50f;
    [Tooltip("If true, dead targets are skipped and NOT revived by the super.")]
    public bool skipDeadTargets = true;

    public int CurrentStack => currentStack;
    public bool IsReady => currentStack >= maxStack;

    private void Awake()
    {
        RefreshButtonVisibility();
    }

    /// <summary>
    /// Call this from whatever action should build up the super
    /// (e.g. each time the player performs a normal heal tap).
    /// </summary>
    public void AddStack(int amount = 1)
    {
        if (IsReady) return; // already charged, waiting to be used

        currentStack = Mathf.Min(currentStack + amount, maxStack);
        RefreshButtonVisibility();
    }

    public void ResetStack()
    {
        currentStack = 0;
        RefreshButtonVisibility();
    }

    private void RefreshButtonVisibility()
    {
        if (superButton != null)
            superButton.SetActive(IsReady);
    }

    /// <summary>
    /// Hook this up to the super button's OnClick event.
    /// </summary>
    public void UseSuper()
    {
        if (!IsReady) return;

        List<HealTargetB> targets = healEntireScene
            ? FindHealableInScene()
            : FindHealableInRadius();

        foreach (HealTargetB target in targets)
        {
            if (target == null) continue;
            if (skipDeadTargets && target.IsDead) continue;

            target.FullyHeal();
        }

        ResetStack();
    }

    private List<HealTargetB> FindHealableInScene()
    {
        var result = new List<HealTargetB>();
        HealTargetB[] all = FindObjectsOfType<HealTargetB>();

        foreach (HealTargetB target in all)
        {
            if (IsOnHealableLayer(target.gameObject))
                result.Add(target);
        }

        return result;
    }

    private List<HealTargetB> FindHealableInRadius()
    {
        var result = new List<HealTargetB>();

        // 3D version. If your game is 2D, swap this for:
        // Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, healRadius, healableLayer);
        Collider[] hits = Physics.OverlapSphere(transform.position, healRadius, healableLayer);

        foreach (Collider hit in hits)
        {
            HealTargetB target = hit.GetComponent<HealTargetB>();
            if (target != null)
                result.Add(target);
        }

        return result;
    }

    private bool IsOnHealableLayer(GameObject obj)
    {
        return ((1 << obj.layer) & healableLayer.value) != 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (healEntireScene) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, healRadius);
    }
}