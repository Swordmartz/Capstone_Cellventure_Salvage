using UnityEngine;

public class AttackRelay : MonoBehaviour
{
    public MeleeAttack character1Melee;
    public MeleeAttack character2Melee;

    public void PerformAttack()
    {
        if (character1Melee.gameObject.activeInHierarchy)
            character1Melee.PerformAttack();
        else if (character2Melee.gameObject.activeInHierarchy)
            character2Melee.PerformAttack();
    }
}