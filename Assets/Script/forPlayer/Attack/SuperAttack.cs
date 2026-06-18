using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class SuperMove : MonoBehaviour
{
    [Header("References")]
    public SliderTimer superBar;
    public GameObject nextCharacter;
    public CinemachineCamera vcam1;
    public CinemachineCamera vcam2;
    public MinimapFollow minimapFollow;
    public string minimapTargetChildName = "MinimapTarget";

    [Header("UI to Deactivate")]
    public GameObject superSliderObject;
    public GameObject superButtonObject;

    [Header("UI to Activate")]
    public GameObject nextUIObject;

    [Header("Super Settings")]
    public float killRadius = 30f;

    public void ActivateSuper()
    {
        if (!superBar.IsFull) return;

        DetectionFSM target = GetNearestMarkedEnemy();
        if (target == null) return;

        target.Die();
        target.ClearMark();
        superBar.ConsumeBar();

        UpdateUI();
        SwitchToNextCharacter();
    }

    private DetectionFSM GetNearestMarkedEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, killRadius);

        DetectionFSM nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            DetectionFSM enemy = hit.GetComponent<DetectionFSM>();
            if (enemy == null || !enemy.isMarked) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private void UpdateUI()
    {
        superSliderObject?.SetActive(false);
        superButtonObject?.SetActive(false);
        nextUIObject?.SetActive(true);
    }

    private void SwitchToNextCharacter()
    {
        nextCharacter?.SetActive(true);

        if (vcam1 != null) vcam1.Priority = 0;
        if (vcam2 != null) vcam2.Priority = 10;

        if (minimapFollow != null && nextCharacter != null)
        {
            Transform minimapTarget = nextCharacter.transform.Find(minimapTargetChildName);
            minimapFollow.player = minimapTarget != null ? minimapTarget : nextCharacter.transform;
        }

        gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, killRadius);
    }
}