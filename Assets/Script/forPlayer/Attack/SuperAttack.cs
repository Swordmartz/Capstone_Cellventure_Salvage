using UnityEngine;

public class SuperMove : MonoBehaviour
{
    [Header("References")]
    public SliderTimer superBar;     // assign in Inspector

    [Header("Super Settings")]
    public float killRadius = 30f;

    // Called by your UI Button OnClick()
    public void ActivateSuper()
    {
        if (!superBar.IsFull)
        {
            Debug.Log("Super bar not full yet!");
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, killRadius);
        int killCount = 0;

        foreach (Collider hit in hits)
        {
            DetectionFSM enemy = hit.GetComponent<DetectionFSM>();
            if (enemy != null && enemy.isMarked)
            {
                enemy.Die();
                enemy.ClearMark();
                killCount++;
            }
        }

        if (killCount > 0)
        {
            superBar.ConsumeBar();
            Debug.Log($"Super activated! {killCount} marked enemies instantly killed.");
            gameObject.SetActive(false); // 👈 player dies after super
        }
        else
        {
            Debug.Log("No marked enemies nearby — super not consumed.");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, killRadius);
    }
}