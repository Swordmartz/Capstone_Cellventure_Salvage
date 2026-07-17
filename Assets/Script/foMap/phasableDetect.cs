using UnityEngine;

// Attach this to the WALL's detection zone — a trigger collider sized a bit
// bigger than the actual solid wall (covering its full thickness plus a small
// margin on both the entry and exit sides).
[RequireComponent(typeof(Collider))]
public class PhasableWall : MonoBehaviour
{
    [Tooltip("The actual solid collider that blocks the player (the physical wall)")]
    public Collider solidCollider;

    [Tooltip("Tag used to identify the player")]
    public string playerTag = "Player";

    // Counts overlapping player colliders in case the player has more than one
    // (e.g. a body collider + a feet collider) so we don't unphase too early.
    private int playersInside = 0;

    void Awake()
    {
        // Detection zone must always be a trigger so the player never gets
        // physically blocked before phasing can even start.
        GetComponent<Collider>().isTrigger = true;

        solidCollider.isTrigger = false; // wall starts solid
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playersInside++;
        solidCollider.isTrigger = true; // let the player pass through
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playersInside = Mathf.Max(0, playersInside - 1);

        // Only solidify once EVERY overlapping player collider has fully left
        // the detection zone — this is what prevents the softlock.
        if (playersInside == 0)
        {
            solidCollider.isTrigger = false;
        }
    }
}