using UnityEngine;

/// <summary>
/// Attach to any cell/entity that can become infected. While IsInfected is
/// true, this counts toward the Inflammation meter; curing it (setting
/// IsInfected back to false) removes its contribution.
/// </summary>
public class InfectedCell : MonoBehaviour
{
    [Tooltip("Starting infection state.")]
    [SerializeField] private bool isInfected = false;

    [Tooltip("How much an infected instance of this cell counts toward inflammation relative to other sources.")]
    [SerializeField, Min(0f)] private float inflammationWeight = 1f;

    /// <summary>Fired whenever infection state changes, with the new value.</summary>
    public event System.Action<bool> OnInfectionStateChanged;

    public bool IsInfected
    {
        get => isInfected;
        set
        {
            if (isInfected == value) return;

            isInfected = value;
            OnInfectionStateChanged?.Invoke(isInfected);
            SyncInflammationRegistration();
        }
    }

    public void Infect() => IsInfected = true;
    public void Cure() => IsInfected = false;

    private void OnEnable()
    {
        SyncInflammationRegistration();
    }

    private void Start()
    {
        // Safety net: if this cell starts already infected and InflammationManager
        // hadn't run its Awake() yet when OnEnable() fired above (Unity doesn't
        // guarantee cross-object Awake/OnEnable ordering), retry here — Start()
        // is guaranteed to run only after every object's Awake() has completed.
        SyncInflammationRegistration();
    }

    private void OnDisable()
    {
        if (InflammationManager.Instance != null)
            InflammationManager.Instance.UnregisterSource(this);
    }

    private void SyncInflammationRegistration()
    {
        if (InflammationManager.Instance == null)
            return;

        if (isInfected && isActiveAndEnabled)
            InflammationManager.Instance.RegisterSource(this, inflammationWeight);
        else
            InflammationManager.Instance.UnregisterSource(this);
    }
}