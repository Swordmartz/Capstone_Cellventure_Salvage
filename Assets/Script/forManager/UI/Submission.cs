using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sequential mission list.
/// - Title stays visible and unchanged at all times.
/// - Description turns green on completion, then fades out.
/// - Next mission's description fades in on the same card.
/// </summary>
public class MissionSubmissionManager : MonoBehaviour
{
    [System.Serializable]
    public class Mission
    {
        [Tooltip("Display name of the mission")]
        public string missionName;

        [Tooltip("Optional description shown below the mission name")]
        public string description;

        [Tooltip("Tick this during Play Mode (or via code) to complete the mission")]
        public bool isCompleted = false;
    }

    [Header("Mission Data")]
    [SerializeField] private List<Mission> missions = new List<Mission>();

    [Header("UI References")]
    [SerializeField] private GameObject missionCardPrefab;
    [SerializeField] private Transform missionContainer;

    [Header("Timing")]
    [SerializeField] private float appearDuration = 0.4f;
    [SerializeField] private float completedHoldTime = 1.2f;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Colors")]
    [SerializeField] private Color descNormalColor = Color.white;
    [SerializeField] private Color descCompletedColor = new Color(0.10f, 0.85f, 0.35f, 1f); // green

    // ── internals ────────────────────────────────────────────────────────────
    private int _currentIndex = 0;
    private bool _waitingForDone = false;
    private bool _allDone = false;

    private GameObject _activeCard;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _descText;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        if (missions == null || missions.Count == 0)
        {
            Debug.LogWarning("[MissionSubmissionManager] No missions defined.");
            return;
        }
        StartCoroutine(ShowNextMission());
    }

    private void Update()
    {
        if (_waitingForDone && _currentIndex < missions.Count && missions[_currentIndex].isCompleted)
        {
            Debug.Log("[Mission] isCompleted detected — completing mission.");
            CompleteMission();
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void CompleteMission()
    {
        if (_allDone || !_waitingForDone) return;
        _waitingForDone = false;
        missions[_currentIndex].isCompleted = true;
        StartCoroutine(HandleMissionCompleted());
    }

    public void CompleteMissionByIndex(int index)
    {
        if (index < 0 || index >= missions.Count) return;
        missions[index].isCompleted = true;
        if (index == _currentIndex && _waitingForDone)
        {
            _waitingForDone = false;
            StartCoroutine(HandleMissionCompleted());
        }
    }

    public bool AreAllMissionsDone() => _allDone;

    // ── Core coroutines ───────────────────────────────────────────────────────

    private IEnumerator ShowNextMission()
    {
        if (_currentIndex >= missions.Count)
        {
            _allDone = true;
            OnAllMissionsCompleted();
            yield break;
        }

        Mission m = missions[_currentIndex];

        // Spawn card once, reuse for all missions
        if (_activeCard == null)
        {
            _activeCard = Instantiate(missionCardPrefab, missionContainer);
            CacheCardReferences(_activeCard);

            // Title is set once and stays visible forever
            if (_titleText)
            {
                _titleText.text = "Missions";   // or any fixed header you prefer
                _titleText.color = Color.white;
            }
        }

        // Update title to current mission name (stays fully visible, no fade)
        if (_titleText)
        {
            _titleText.text = m.missionName;
            _titleText.color = Color.black;
        }

        // Reset description: transparent, normal color, new text
        if (_descText)
        {
            _descText.text = m.description;
            _descText.color = new Color(descNormalColor.r, descNormalColor.g, descNormalColor.b, 0f);
        }


        // Fade description IN only
        yield return StartCoroutine(FadeDesc(0f, 1f, appearDuration, descNormalColor));

        if (m.isCompleted)
            yield return StartCoroutine(HandleMissionCompleted());
        else
        {
            _waitingForDone = true;
            yield return new WaitUntil(() => !_waitingForDone);
        }
    }

    private IEnumerator HandleMissionCompleted()
    {
        // Description turns green
        yield return StartCoroutine(AnimateDescToGreen());

        // Hold so player reads it
        yield return new WaitForSeconds(completedHoldTime);

        // Fade description OUT only — title stays
        yield return StartCoroutine(FadeDesc(1f, 0f, fadeDuration, descCompletedColor));


        _currentIndex++;

        yield return new WaitForSeconds(0.15f);

        StartCoroutine(ShowNextMission());
    }

    // ── Animation helpers ─────────────────────────────────────────────────────

    /// Fades only the description alpha from <from> to <to>, keeping the given base color.
    private IEnumerator FadeDesc(float from, float to, float duration, Color baseColor)
    {
        if (_descText == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float alpha = Mathf.Lerp(from, to, t);
            _descText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }
        _descText.color = new Color(baseColor.r, baseColor.g, baseColor.b, to);
    }

    /// Transitions description color from white to green over 0.35s.
    private IEnumerator AnimateDescToGreen()
    {
        if (_descText == null) yield break;

        Color startColor = _descText.color;
        Color targetColor = new Color(descCompletedColor.r, descCompletedColor.g, descCompletedColor.b, 1f);

        float elapsed = 0f;
        float duration = 0.35f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _descText.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        _descText.color = targetColor;
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private void CacheCardReferences(GameObject card)
    {
        Transform titleT = card.transform.Find("Title");
        Transform descT = card.transform.Find("Description");

        _titleText = titleT ? titleT.GetComponent<TextMeshProUGUI>() : null;
        _descText = descT ? descT.GetComponent<TextMeshProUGUI>() : null;
    }

    protected virtual void OnAllMissionsCompleted()
    {
        Debug.Log("[MissionSubmissionManager] All missions completed!");
        onAllMissionsCompleted?.Invoke();
    }

    [Header("Events")]
    public UnityEngine.Events.UnityEvent onAllMissionsCompleted;
}