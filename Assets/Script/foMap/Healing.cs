using UnityEngine;
using UnityEngine.UI;

public class HealButton : MonoBehaviour
{
    public Transform player;
    public Slider hpSlider;

    public GameObject interactButton;
    public GameObject healCanvas;

    private HealTargetB currentTarget;
    private bool isInteracting = false;

    private void Update()
    {
        HealTargetB nearest = GetNearestInRange();

        // Target changed or left range
        if (nearest != currentTarget)
        {
            currentTarget = nearest;
            isInteracting = false;
            if (interactButton != null) interactButton.SetActive(false);
            if (healCanvas != null) healCanvas.SetActive(false);
        }

        if (currentTarget == null) return;

        if (!isInteracting)
            if (interactButton != null) interactButton.SetActive(true);

        if (hpSlider != null)
            hpSlider.value = currentTarget.CurrentHP / currentTarget.maxHP;
    }

    public void OnInteractButton()
    {
        if (currentTarget == null) return;
        isInteracting = true;
        if (interactButton != null) interactButton.SetActive(false);
        if (healCanvas != null) healCanvas.SetActive(true);
    }

    public void OnTapButton()
    {
        if (currentTarget == null) return;
        currentTarget.Tap();
    }

    private HealTargetB GetNearestInRange()
    {
        HealTargetB[] allTargets = FindObjectsByType<HealTargetB>(FindObjectsSortMode.None);
        HealTargetB nearest = null;
        float closestDist = Mathf.Infinity;

        foreach (HealTargetB target in allTargets)
        {
            if (target.IsFullyHealed || target.IsDead) continue;

            float dist = Vector3.Distance(player.position, target.transform.position);
            if (dist <= target.healRange && dist < closestDist)
            {
                closestDist = dist;
                nearest = target;
            }
        }

        return nearest;
    }
}