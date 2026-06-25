using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperEat : MonoBehaviour
{
    [Header("References")]
    public SliderTimer superBar;

    [Header("Suck Settings")]
    public float suckRadius = 40f;
    public float suckSpeed = 10f;

    [Header("Eat Capsule Detection")]
    public float capsuleRadius = 1.5f;
    public float capsuleHeight = 3f;
    public Vector3 capsuleOffset = new Vector3(0f, 0f, 0f);

    [Header("Duration")]
    public float eatDuration = 3f;

    private bool isEating = false;

    public void ActivateSuperEat()
    {
        if (!superBar.IsFull) return;

        List<DetectionFSM> deadEnemies = GetDeadEnemiesInRadius();
        if (deadEnemies.Count == 0) return;

        StartCoroutine(SuckEnemies(deadEnemies));
        superBar.ConsumeBar();
    }

    private List<DetectionFSM> GetDeadEnemiesInRadius()
    {
        List<DetectionFSM> dead = new List<DetectionFSM>();
        Collider[] hits = Physics.OverlapSphere(transform.position, suckRadius, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            DetectionFSM enemy = hit.GetComponent<DetectionFSM>();
            if (enemy != null && enemy.currentState == DetectionFSM.EnemyState.Dead)
                dead.Add(enemy);
        }

        return dead;
    }

    private IEnumerator SuckEnemies(List<DetectionFSM> enemies)
    {
        isEating = true;
        float elapsed = 0f;

        List<DetectionFSM> active = new List<DetectionFSM>(enemies);

        while (active.Count > 0 && elapsed < eatDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                DetectionFSM enemy = active[i];

                if (enemy == null || !enemy.gameObject.activeSelf)
                {
                    active.RemoveAt(i);
                    continue;
                }

                enemy.transform.position = Vector3.MoveTowards(
                    enemy.transform.position,
                    transform.position,
                    suckSpeed * Time.deltaTime
                );

                if (IsInsideCapsule(enemy.transform.position))
                {
                    enemy.gameObject.SetActive(false);
                    active.RemoveAt(i);
                }
            }

            yield return null;
        }

        isEating = false;
    }

    private bool IsInsideCapsule(Vector3 point)
    {
        Vector3 center = transform.position + capsuleOffset;
        float halfHeight = Mathf.Max(0f, (capsuleHeight / 2f) - capsuleRadius);

        Vector3 pointA = center + Vector3.up * halfHeight;
        Vector3 pointB = center - Vector3.up * halfHeight;

        Vector3 ab = pointB - pointA;
        Vector3 ap = point - pointA;

        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Vector3.Dot(ab, ab));
        Vector3 closest = pointA + t * ab;

        return (point - closest).sqrMagnitude <= capsuleRadius * capsuleRadius;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, suckRadius);

        Vector3 center = transform.position + capsuleOffset;
        float halfHeight = Mathf.Max(0f, (capsuleHeight / 2f) - capsuleRadius);
        Vector3 pointA = center + Vector3.up * halfHeight;
        Vector3 pointB = center - Vector3.up * halfHeight;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pointA, capsuleRadius);
        Gizmos.DrawWireSphere(pointB, capsuleRadius);
        Gizmos.DrawLine(pointA + Vector3.left * capsuleRadius, pointB + Vector3.left * capsuleRadius);
        Gizmos.DrawLine(pointA + Vector3.right * capsuleRadius, pointB + Vector3.right * capsuleRadius);
        Gizmos.DrawLine(pointA + Vector3.forward * capsuleRadius, pointB + Vector3.forward * capsuleRadius);
        Gizmos.DrawLine(pointA + Vector3.back * capsuleRadius, pointB + Vector3.back * capsuleRadius);
    }
}