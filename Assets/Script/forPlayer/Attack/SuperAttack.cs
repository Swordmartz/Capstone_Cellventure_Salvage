using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using System.Collections;

public class SuperMove : MonoBehaviour
{
    [Header("References")]
    public SliderTimer superBar;
    public GameObject nextCharacter;
    public CinemachineCamera vcam1;
    public CinemachineCamera vcam2;
    public MinimapFollow minimapFollow;
    public string minimapTargetChildName = "MinimapTarget";
    public AIforDialogue aiScript;

    [Header("UI to Deactivate")]
    public GameObject[] objectsToDeactivate;

    [Header("UI to Activate")]
    public GameObject[] objectsToActivate;

    [Header("Super Settings")]
    public float killRadius = 30f;

    [Header("Camera Switch Settings")]
    [Tooltip("Delay (in seconds) before the camera priority swap (and teleport) happens. Adjustable in Inspector.")]
    public float cameraSwitchDelay = 0.5f;

    [Header("Poof Effect")]
    [Tooltip("Drag your poof particle system prefab here.")]
    public GameObject poofPrefab;

    [Tooltip("Vertical offset so the poof appears at chest/center level.")]
    public Vector3 poofOffset = new Vector3(0f, 1f, 0f);

    [Header("Character Visuals")]
    [Tooltip("Optional: the visual model/root of this character to hide instantly when the poof plays. " +
             "If left empty, all Renderers on this object/children will be disabled instead.")]
    public GameObject characterModel;

    public void ActivateSuper()
    {
        if (!superBar.IsFull) return;

        Component target = GetNearestKillableEnemy();
        if (target == null) return;

        // DetectionFSM enemies must be marked first, and use their own
        // Die()/ClearMark() pair like before.
        if (target is DetectionFSM detectionTarget)
        {
            if (!detectionTarget.isMarked) return;
            detectionTarget.Die();
            detectionTarget.ClearMark();
        }
        // InfluenzaFSM now has its own isMarked/ClearMark, mirroring
        // DetectionFSM, so it's killed and un-marked the same way.
        else if (target is InfluenzaFSM influenzaTarget)
        {
            if (!influenzaTarget.isMarked) return;
            influenzaTarget.TakeDamage(float.MaxValue);
            influenzaTarget.ClearMark();
        }
        // pneumonococcalFSM also has its own isMarked/ClearMark now, so it's
        // killed and un-marked the same way — its TakeDamage takes an int,
        // so int.MaxValue is used instead of float.MaxValue.
        else if (target is pneumonococcalFSM pneumonococcalTarget)
        {
            if (!pneumonococcalTarget.isMarked) return;
            pneumonococcalTarget.TakeDamage(int.MaxValue);
            pneumonococcalTarget.ClearMark();
        }
        // MalariaFSM uses IsMarked/SetMarked instead of isMarked/ClearMark
        // (property + setter rather than a public field + separate clear
        // method), and Kill() instantly zeroes its HP and moves it straight
        // to State.Dead rather than needing a huge TakeDamage() amount.
        else if (target is MalariaFSM malariaTarget)
        {
            if (!malariaTarget.IsMarked) return;
            malariaTarget.Kill();
            malariaTarget.SetMarked(false);
        }
        else
        {
            // Not a recognized enemy type at all — nothing to kill.
            return;
        }

        UpdateUI();            // ← UI first
        superBar.ConsumeBar();  // ← then reset bar (won't fight UpdateUI)
        SwitchToNextCharacter();
    }

    // Finds the nearest killable enemy across all enemy types: DetectionFSM,
    // InfluenzaFSM, pneumonococcalFSM, and MalariaFSM enemies must all be
    // marked to count.
    private Component GetNearestKillableEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, killRadius);

        Component nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist >= nearestDist) continue;

            DetectionFSM detectionEnemy = hit.GetComponent<DetectionFSM>();
            if (detectionEnemy != null && detectionEnemy.isMarked)
            {
                nearest = detectionEnemy;
                nearestDist = dist;
                continue;
            }

            InfluenzaFSM influenzaEnemy = hit.GetComponent<InfluenzaFSM>();
            if (influenzaEnemy != null && influenzaEnemy.isMarked)
            {
                nearest = influenzaEnemy;
                nearestDist = dist;
                continue;
            }

            pneumonococcalFSM pneumonococcalEnemy = hit.GetComponent<pneumonococcalFSM>();
            if (pneumonococcalEnemy != null && pneumonococcalEnemy.isMarked)
            {
                nearest = pneumonococcalEnemy;
                nearestDist = dist;
                continue;
            }

            MalariaFSM malariaEnemy = hit.GetComponent<MalariaFSM>();
            if (malariaEnemy != null && malariaEnemy.IsMarked)
            {
                nearest = malariaEnemy;
                nearestDist = dist;
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

    private void HideCharacterVisuals()
    {
        if (characterModel != null)
        {
            characterModel.SetActive(false);
            return;
        }

        // Fallback: hide all renderers so the character visually vanishes
        // without disabling the whole GameObject (which would kill this coroutine).
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
    }

    private void SwitchToNextCharacter()
    {
        // Poof + hiding the old character happen immediately, together, IN PLACE.
        // We deliberately do NOT move transform.position or touch camera priorities
        // yet — vcam1 is still active and following this transform, so moving it
        // now would make the camera jump early.
        SpawnPoof();
        HideCharacterVisuals();

        StartCoroutine(FinishSwitchAfterDelay(cameraSwitchDelay));
    }

    private IEnumerator FinishSwitchAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // Play the dialogue and wait for it to fully finish before touching
        // the camera priority swap below.
        if (aiScript != null)
            yield return aiScript.StartCoroutine(aiScript.Dialogue4IWBCE());
        else
            Debug.LogWarning("AI script not assigned!");

        // Teleport and camera swap happen together, on the same frame,
        // so there's no in-between frame where things look mismatched.
        if (nextCharacter != null)
        {
            transform.position = nextCharacter.transform.position;
            nextCharacter.SetActive(true);
        }

        if (minimapFollow != null && nextCharacter != null)
        {
            Transform minimapTarget = nextCharacter.transform.Find(minimapTargetChildName);
            minimapFollow.player = minimapTarget != null ? minimapTarget : nextCharacter.transform;
        }

        if (vcam1 != null) vcam1.Priority = 0;
        if (vcam2 != null) vcam2.Priority = 10;

        if (aiScript != null)
            aiScript.StartCoroutine(aiScript.Dialogue5IWBCE());
        else
            Debug.LogWarning("AI script not assigned!");

        gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, killRadius);
    }
}