using UnityEngine;

public class MissionInventoryChecker : MonoBehaviour
{
    [Header("References")]
    public Inventory playerInventory;
    public MissionSubmissionManager missionManager;

    [Header("Settings")]
    [Tooltip("The item that must be in the inventory to complete the mission.")]
    public O2Item requiredItem;

    private bool _missionCompleted = false;

    private void Update()
    {
        if (_missionCompleted) return;

        if (playerInventory != null && playerInventory.HasItem)
        {
            _missionCompleted = true;
            missionManager.CompleteMissionByIndex(1);
            Debug.Log("[MissionInventoryChecker] Required item found — Mission 1 completed.");
        }
    }
}