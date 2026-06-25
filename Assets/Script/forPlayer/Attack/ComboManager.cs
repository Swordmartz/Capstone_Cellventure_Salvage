using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComboCounterUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The SpearMeleeAttack script on the player.")]
    public SpearMeleeAttack spearAttack;

    [Tooltip("Displays the combo number, e.g. 'x7'.")]
    public TextMeshProUGUI comboCountText;

    [Tooltip("Optional label shown above/below the number, e.g. 'COMBO'.")]
    public TextMeshProUGUI comboLabelText;

    [Tooltip("Optional: the root CanvasGroup or GameObject of the combo panel for fade in/out.")]
    public CanvasGroup comboPanel;

    [Tooltip("Optional: an Animator on the combo panel for punch/shake animations.")]
    public Animator comboAnimator;

    [Header("Super Bar")]
    [Tooltip("Original super bar reference (used in other scenes).")]
    public SuperAttackBarUI superAttackBar;

    [Tooltip("SliderTimer-based super bar for scenes that use SliderTimer.")]
    public SliderTimer sliderSuperBar;

    [Header("Settings")]
    [Tooltip("Seconds of no hits before the combo resets to 0.")]
    public float comboResetDelay = 2f;

    [Tooltip("Combo milestone thresholds that trigger a color change AND add charge to the super bar (e.g. 5, 10, 20).")]
    public int[] milestones = { 5, 10, 20 };

    [Tooltip("Colors corresponding to each milestone (must match milestones array length).")]
    public Color[] milestoneColors = { Color.yellow, Color.cyan, Color.red };

    [Tooltip("Fixed charge added to the super attack bar each time a milestone is reached.")]
    public float superChargePerMilestone = 25f;

    [Tooltip("Default color of the combo number text.")]
    public Color defaultColor = Color.white;

    [Header("Animation")]
    [Tooltip("Name of the Animator trigger to fire on each new hit.")]
    public string hitAnimTrigger = "Hit";

    [Tooltip("Name of the Animator trigger to fire on combo reset.")]
    public string resetAnimTrigger = "Reset";

    [Header("Fade")]
    [Tooltip("How fast the combo panel fades in when a hit lands.")]
    public float fadeInDuration = 0.15f;
    [Tooltip("How fast the combo panel fades out after the combo resets.")]
    public float fadeOutDuration = 0.4f;

    // ── Internal state ──────────────────────────────────────────────────
    private int comboCount = 0;
    private int lastKnownStack = 0;
    private float lastHitTime = -999f;
    private Coroutine resetCoroutine;
    private Coroutine fadeCoroutine;

    // ── Unity lifecycle ─────────────────────────────────────────────────
    void Start()
    {
        if (spearAttack == null)
            Debug.LogError("[ComboCounterUI] spearAttack is not assigned!");

        lastKnownStack = spearAttack != null ? spearAttack.rapidAttackStack : 0;

        if (comboPanel != null)
            comboPanel.alpha = 0f;

        RefreshUI();
    }

    void Update()
    {
        if (spearAttack == null) return;

        int currentStack = spearAttack.rapidAttackStack;

        if (currentStack > lastKnownStack)
        {
            int newHits = currentStack - lastKnownStack;
            for (int i = 0; i < newHits; i++)
                RegisterHit();
        }

        lastKnownStack = currentStack;
    }

    // ── Core logic ───────────────────────────────────────────────────────
    private void RegisterHit()
    {
        comboCount++;
        lastHitTime = Time.time;

        // Feed whichever super bar is assigned
        foreach (int milestone in milestones)
        {
            if (comboCount == milestone)
            {
                if (superAttackBar != null)
                    superAttackBar.AddCharge(superChargePerMilestone);

                if (sliderSuperBar != null)
                    sliderSuperBar.AddCharge(superChargePerMilestone);

                break;
            }
        }

        RefreshUI();
        PlayHitAnimation();

        if (comboPanel != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePanel(1f, fadeInDuration));
        }

        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);
        resetCoroutine = StartCoroutine(ResetAfterDelay());
    }

    private void ResetCombo()
    {
        comboCount = 0;
        PlayResetAnimation();
        RefreshUI();

        if (comboPanel != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePanel(0f, fadeOutDuration));
        }
    }

    // ── UI refresh ───────────────────────────────────────────────────────
    private void RefreshUI()
    {
        if (comboCountText != null)
        {
            comboCountText.text = comboCount > 0 ? $"x{comboCount}" : "";
            comboCountText.color = GetComboColor();
        }

        if (comboLabelText != null)
            comboLabelText.gameObject.SetActive(comboCount > 1);
    }

    private Color GetComboColor()
    {
        for (int i = milestones.Length - 1; i >= 0; i--)
        {
            if (comboCount >= milestones[i] && i < milestoneColors.Length)
                return milestoneColors[i];
        }
        return defaultColor;
    }

    // ── Animation helpers ────────────────────────────────────────────────
    private void PlayHitAnimation()
    {
        if (comboAnimator != null && !string.IsNullOrEmpty(hitAnimTrigger))
            comboAnimator.SetTrigger(hitAnimTrigger);
    }

    private void PlayResetAnimation()
    {
        if (comboAnimator != null && !string.IsNullOrEmpty(resetAnimTrigger))
            comboAnimator.SetTrigger(resetAnimTrigger);
    }

    // ── Coroutines ───────────────────────────────────────────────────────
    private IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(comboResetDelay);
        ResetCombo();
    }

    private IEnumerator FadePanel(float targetAlpha, float duration)
    {
        float startAlpha = comboPanel.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            comboPanel.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        comboPanel.alpha = targetAlpha;
        fadeCoroutine = null;
    }

    // ── Public API ────────────────────────────────────────────────────────
    public int CurrentCombo => comboCount;

    public void RegisterExternalHit()
    {
        RegisterHit();
    }

    public void ForceReset()
    {
        if (resetCoroutine != null) StopCoroutine(resetCoroutine);
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        ResetCombo();
    }
}