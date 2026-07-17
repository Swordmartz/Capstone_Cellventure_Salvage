using UnityEngine;

public class IsInfected : MonoBehaviour
{
    [SerializeField] private bool isInfected;

    public bool Infected => isInfected;

    public void SetInfected(bool value)
    {
        isInfected = value;
    }
}