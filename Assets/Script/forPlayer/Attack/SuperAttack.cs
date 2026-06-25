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
    public GameObject[] objectsToDeactivate;

    [Header("UI to Activate")]
    public GameObject[] objectsToActivate;

    [Header("Super Settings")]
    public float killRadius = 30f;

    [Header("Poof Effect")]
    [Tooltip("Drag your poof particle system prefab here.")]
    public GameObject poofPrefab;

    [Tooltip("Vertical offset so the poof appears at chest/center level.")]
    public Vector3 poofOffset = new Vector3(0f, 1f, 0f);

    public void ActivateSuper()
    {
        if (!superBar.IsFull) return;

        DetectionFSM target = GetNearestMarkedEnemy();
        if (target == null) return;

        target.Die();
        target.ClearMark();

        UpdateUI();           // ← UI first
        superBar.ConsumeBar(); // ← then reset bar (won't fight UpdateUI)
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
        foreach (GameObject obj in objectsToDeactivate)
            obj?.SetActive(false);

        foreach (GameObject obj in objectsToActivate)
            obj?.SetActive(true);
    }

    private void SpawnPoof()
    {
        if (poofPrefab == null) return;

        GameObject poof = Instantiate(poofPrefab, transform.position + poofOffset, Quaternion.identity);

        ParticleSystem ps = poof.GetComponent<ParticleSystem>();
        if (ps != null)
            Destroy(poof, ps.main.duration + ps.main.startLifetime.constantMax);
        else
            Destroy(poof, 2f);
    }

    private void SwitchToNextCharacter()
    {
        SpawnPoof();

        if (nextCharacter != null)
        {
            transform.position = nextCharacter.transform.position;
            nextCharacter.SetActive(true);
        }

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