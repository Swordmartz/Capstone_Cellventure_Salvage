using UnityEngine;
using UnityEngine.UI;

public class RapidAttackStackUI : MonoBehaviour
{
    [Header("References")]
    public SpearMeleeAttack spearAttack;
    public Slider stackSlider;
    public Button stackButton;

    [Header("Settings")]
    public int maxStack = 5;

    void Start()
    {
        if (stackSlider != null)
        {
            stackSlider.minValue = 0;
            stackSlider.maxValue = maxStack;
        }

        if (stackButton != null)
            stackButton.onClick.AddListener(OnStackButtonClicked);

        RefreshUI();
    }

    void Update()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (spearAttack == null) return;

        int currentStack = Mathf.Min(spearAttack.rapidAttackStack, maxStack);

        if (stackSlider != null)
            stackSlider.value = currentStack;

        if (stackButton != null)
            stackButton.interactable = currentStack >= maxStack;
    }

    private void OnStackButtonClicked()
    {
        Debug.Log("[RapidAttackStackUI] Button pressed — working correctly!");
    }
}