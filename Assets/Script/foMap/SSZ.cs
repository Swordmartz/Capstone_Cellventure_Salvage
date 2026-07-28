using UnityEngine;

/// <summary>
/// Place this on a GameObject with a 3D Collider (Is Trigger = true) positioned
/// wherever RBC should switch sprites — regardless of which spline it's on, or that
/// spline's length/knot count. Works for any number of zones on any number of splines.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SpriteSwapZone : MonoBehaviour
{
    [Tooltip("Sprite RBC will switch to when it enters this zone.")]
    [SerializeField] private Sprite newSprite;

    [Tooltip("Only objects with this tag will trigger the swap. Set to RBC's tag, or leave empty to allow any object with the switcher component.")]
    [SerializeField] private string requiredTag = "";

    [Tooltip("If true, this zone can only trigger once, then disables itself.")]
    [SerializeField] private bool oneShot = true;

    private bool hasFired;

    private void Reset()
    {
        // Make sure the collider is set up as a trigger by default.
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[SpriteSwapZone] OnTriggerEnter hit by {other.name}, oneShot={oneShot}, hasFired={hasFired}");

        if (oneShot && hasFired) return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
        {
            Debug.Log($"[SpriteSwapZone] Tag mismatch: {other.tag} != {requiredTag}");
            return;
        }

        // Use GetComponentInParent instead of GetComponent, in case the Collider
        // is on a child object separate from where RBCSplineSpriteSwitcher lives.
        var rbc = other.GetComponentInParent<RBCSplineSpriteSwitcher>();
        if (rbc == null)
        {
            Debug.Log($"[SpriteSwapZone] No RBCSplineSpriteSwitcher found on {other.name} or its parents");
            return;
        }

        if (newSprite == null)
        {
            Debug.LogWarning($"[SpriteSwapZone] newSprite is not assigned on {gameObject.name}!");
        }

        Debug.Log($"[SpriteSwapZone] Calling SwapSprite({newSprite?.name ?? "NULL"}) on {rbc.gameObject.name}");
        rbc.SwapSprite(newSprite);
        hasFired = true;
    }
}