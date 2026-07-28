using UnityEngine;
using Unity.Cinemachine;

public class CharacterSwitchManager : MonoBehaviour
{
    [Header("Characters")]
    public GameObject character1;
    public GameObject character2;
    public GameObject character3;
    [Tooltip("Optional — leave empty if you only have 3 characters. If assigned, it's included in the switch cycle like the others.")]
    public GameObject character4;

    [Header("Extra Objects (also toggled on switch)")]
    public GameObject extraObject1;
    public GameObject extraObject2;
    public GameObject extraObject3;
    public GameObject extraObject4;

    [Header("Cameras (CM Cameras)")]
    public CinemachineCamera camera1;
    public CinemachineCamera camera2;
    public CinemachineCamera camera3;
    public CinemachineCamera camera4;

    [Header("Timers")]
    public GameTimer missionTimer;
    public GameTimer missionTimer2;
    public GameTimer missionTimer3;
    public GameTimer missionTimer4;

    [Header("Priority Settings")]
    public int activePriority = 10;
    public int inactivePriority = 0;

    [Header("Player Reference")]
    public GameObject player;

    // Internal arrays built from the inspector fields above
    private GameObject[] characters;
    private GameObject[] extraObjects;
    private CinemachineCamera[] cameras;
    private GameTimer[] timers;

    private int activeIndex = 0;
    private bool initialized = false;

    private void Awake()
    {
        EnsureInitialized();
    }

    // Safe to call from anywhere, any time — builds arrays once,
    // even if another script calls a public method before this Awake() runs.
    private void EnsureInitialized()
    {
        if (initialized) return;

        // character4/extraObject4/camera4/missionTimer4 are optional — leaving
        // them unassigned in the Inspector just means SwitchCharacter() cycles
        // through the first 3 as before, since every array access below is
        // already null-checked. Assign character4 to actually add a 4th slot
        // to the cycle.
        characters = new GameObject[] { character1, character2, character3, character4 };
        extraObjects = new GameObject[] { extraObject1, extraObject2, extraObject3, extraObject4 };
        cameras = new CinemachineCamera[] { camera1, camera2, camera3, camera4 };
        timers = new GameTimer[] { missionTimer, missionTimer2, missionTimer3, missionTimer4 };

        initialized = true;

        // Make sure state matches activeIndex on startup
        ApplySwitch(activeIndex);
    }

    // Assign this method to your UI Button's OnClick() event, or call it from another script.
    // Can be called unlimited times — cycles 1 -> 2 -> 3 -> 4 -> 1 -> 2 -> 3 -> 4 -> ...
    // (or 1 -> 2 -> 3 -> 1 -> ... if character4 is left unassigned — see note below)
    public void SwitchCharacter()
    {
        EnsureInitialized();
        int nextIndex = (activeIndex + 1) % characters.Length;

        // Only the optional 4th slot gets skipped when unassigned. Slots 1-3
        // always stay in the cycle regardless of whether their `character`
        // field happens to be empty — camera/timer for that slot may still
        // be fully wired up, and skipping them would be a behavior change
        // from before character4 was added.
        if (nextIndex == 3 && character4 == null)
            nextIndex = (nextIndex + 1) % characters.Length;

        ApplySwitch(nextIndex);
    }

    // Optional: call this directly if you want a specific character
    // e.g. SwitchToIndex(0) = character1, SwitchToIndex(1) = character2,
    // SwitchToIndex(2) = character3, SwitchToIndex(3) = character4
    public void SwitchToIndex(int index)
    {
        EnsureInitialized();

        if (index < 0 || index >= characters.Length)
        {
            Debug.LogWarning($"[CharacterSwitchManager] SwitchToIndex: index {index} out of range.");
            return;
        }
        ApplySwitch(index);
    }

    // Handy for other scripts that need to know which character is currently active
    public int GetActiveIndex()
    {
        EnsureInitialized();
        return activeIndex;
    }

    public GameObject GetActiveCharacter()
    {
        EnsureInitialized();
        return characters[activeIndex];
    }

    private void ApplySwitch(int newIndex)
    {
        // Pause the timer belonging to the character we're switching away from
        // (skip on first-ever call, since activeIndex == newIndex on startup)
        if (activeIndex != newIndex && timers[activeIndex] != null)
        {
            timers[activeIndex].StopTimer();
        }

        for (int i = 0; i < characters.Length; i++)
        {
            bool isActive = (i == newIndex);

            if (cameras[i] != null)
                cameras[i].Priority = isActive ? activePriority : inactivePriority;

            if (characters[i] != null)
                characters[i].SetActive(isActive);

            if (extraObjects[i] != null)
                extraObjects[i].SetActive(isActive);
        }

        // Resume the timer for the character we're switching to
        if (timers[newIndex] != null)
        {
            timers[newIndex].ResumeTimer();
        }

        player = characters[newIndex];
        activeIndex = newIndex;

        Debug.Log($"[CharacterSwitchManager] Switched to character index {newIndex} ({(characters[newIndex] != null ? characters[newIndex].name : "null")}).");
    }
}