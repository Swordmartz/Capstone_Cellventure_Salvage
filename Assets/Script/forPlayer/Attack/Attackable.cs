using UnityEngine;

public class Attackable : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
    public int currentHealth;

    [Header("Mission Reference")]
    public AI_TestTD missionData;

    public bool IsDead { get; private set; }
    public bool WasHit { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;
        IsDead = false;
        WasHit = false;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        WasHit = true;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            IsDead = true;

            if (missionData != null)
                missionData.AttackableDied++;
        }
    }

    public void ResetHitFlag()
    {
        WasHit = false;
    }
}