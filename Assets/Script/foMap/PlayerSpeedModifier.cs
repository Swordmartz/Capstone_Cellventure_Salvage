using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to the Player GameObject.
/// Any number of SlowZone triggers (or other effects) can register a speed
/// multiplier here. They stack multiplicatively and remove themselves cleanly,
/// so overlapping zones never leave the player stuck at the wrong speed.
/// </summary>
public class PlayerSpeedModifier : MonoBehaviour
{
    private readonly Dictionary<object, float> activeModifiers = new Dictionary<object, float>();

    /// Current combined multiplier. 1 = normal speed, 0.5 = half speed, 0 = stopped.
    public float SpeedMultiplier { get; private set; } = 1f;

    public void AddModifier(object source, float multiplier)
    {
        activeModifiers[source] = multiplier;
        Recalculate();
    }

    public void RemoveModifier(object source)
    {
        if (activeModifiers.Remove(source))
            Recalculate();
    }

    private void Recalculate()
    {
        float result = 1f;
        foreach (var m in activeModifiers.Values)
            result *= m;
        SpeedMultiplier = result;
    }
}