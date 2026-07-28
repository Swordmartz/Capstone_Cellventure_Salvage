using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger zone that slows players by a fixed multiplier while they're inside it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SlowZone : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Only objects with this tag will be slowed.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Slow Strength")]
    [Tooltip("Speed multiplier applied to players while inside the zone. 1 = normal speed, 0.5 = half speed.")]
    [Range(0f, 1f)]
    [SerializeField] private float slowMultiplier = 0.5f;

    private readonly List<PlayerSpeedModifier> playersInZone =
        new List<PlayerSpeedModifier>();

    public float SlowMultiplier => slowMultiplier;

    private void Reset()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        PlayerSpeedModifier modifier =
            other.GetComponent<PlayerSpeedModifier>();

        if (modifier == null)
            return;

        modifier.AddModifier(this, slowMultiplier);

        if (!playersInZone.Contains(modifier))
            playersInZone.Add(modifier);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        PlayerSpeedModifier modifier =
            other.GetComponent<PlayerSpeedModifier>();

        if (modifier == null)
            return;

        modifier.RemoveModifier(this);
        playersInZone.Remove(modifier);
    }
}