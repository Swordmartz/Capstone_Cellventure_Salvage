using UnityEngine;

public class Attackable : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
    public int currentHealth;

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

        WasHit = true; // 🔥 important

        currentHealth -= amount;
        Debug.Log($"{name} took {amount} damage");

        if (currentHealth <= 0)
        {
            IsDead = true;
            Debug.Log($"{name} died");
        }
    }

    public void ResetHitFlag()
    {
        WasHit = false;
    }
}