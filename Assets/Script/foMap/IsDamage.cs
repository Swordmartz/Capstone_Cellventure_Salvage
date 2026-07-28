using UnityEngine;

public class IsDamage : MonoBehaviour
{
    [Header("Damage State")]
    public bool isDamage = false;

    [Header("Count Settings")]
    public int currentCount = 0;
    public int maxCount = 5;

    // Call this from an enemy's FSM when it starts staying at this object
    public void IncreaseCount()
    {
        currentCount++;
        currentCount = Mathf.Clamp(currentCount, 0, maxCount);

        if (currentCount >= maxCount)
        {
            isDamage = true;
        }
    }

    // Call this from an enemy's FSM when it stops staying at this object
    public void DecreaseCount()
    {
        currentCount--;
        currentCount = Mathf.Clamp(currentCount, 0, maxCount);

        if (currentCount < maxCount)
        {
            isDamage = false;
        }
    }
}