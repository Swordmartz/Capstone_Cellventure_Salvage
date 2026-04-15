using UnityEngine;
using UnityEngine.UI;

public class DeliverySlider : MonoBehaviour
{
    [Header("References")]
    public Slider deliverySlider;       // ✅ Drag your UI Slider here
    public AI_TestTD missionSystem;     // ✅ Drag your AI_TestTD script here

    private void Start()
    {
        if (deliverySlider != null && missionSystem != null)
        {
            // Set slider max to delivery threshold
            deliverySlider.maxValue = missionSystem.deliveryThreshold;
            deliverySlider.value = missionSystem.itemsDelivered;
        }
    }

    private void Update()
    {
        if (deliverySlider != null && missionSystem != null)
        {
            // Continuously sync slider with items delivered
            deliverySlider.value = missionSystem.itemsDelivered;
        }
    }
}
