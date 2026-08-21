using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles Next/Prev navigation for an almanac made of "Page 1" entries,
/// plus an optional "Page 2" detail panel that can pop up on top of Page 1.
///
/// ASSUMPTIONS (adjust if your setup differs):
/// - "pages" is an array of GameObjects, each representing one Page 1 screen
///   (e.g. Page[0] = intro, Page[1] = chapter 2, etc). Only one is active at a time.
/// - "detailPanel" is a single GameObject representing Page 2 (e.g. a popup with
///   extra info about something the player clicked on the current Page 1).
///   Whatever data/content it shows should be set by whatever script calls
///   OpenDetailPanel() — this script only handles show/hide + navigation rules.
/// - Pressing Next or Prev ALWAYS closes the detail panel, regardless of whether
///   it was open, since it belongs to the page you're leaving.
/// - Prev button starts disabled (you're on page 0) and becomes enabled as soon
///   as you move forward. It disables again once you return to page 0.
/// - Next button mirrors this behavior at the last page (disables on last page,
///   re-enables once you move back). Delete that part if Next should always
///   stay interactable / loop instead.
/// </summary>
public class AlmanacPageController : MonoBehaviour
{
    [Header("Navigation Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;

    [Header("Page 1 - main almanac pages (only one active at a time)")]
    [SerializeField] private GameObject[] pages;

    [Header("Page 2 - detail panel (can be active alongside a Page 1)")]
    [SerializeField] private GameObject detailPanel;

    private int currentPageIndex = 0;

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextPressed);

        if (prevButton != null)
            prevButton.onClick.AddListener(OnPrevPressed);
    }

    private void Start()
    {
        currentPageIndex = 0;
        ShowOnlyPage(currentPageIndex);

        if (detailPanel != null)
            detailPanel.SetActive(false);

        UpdateButtonStates();
    }

    public void OnNextPressed()
    {
        if (currentPageIndex < pages.Length - 1)
        {
            currentPageIndex++;
            ShowOnlyPage(currentPageIndex);
        }

        CloseDetailPanel();
        UpdateButtonStates();
    }

    public void OnPrevPressed()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowOnlyPage(currentPageIndex);
        }

        CloseDetailPanel();
        UpdateButtonStates();
    }

    /// <summary>
    /// Call this from wherever a Page 1 entry is clicked to bring up the detail panel.
    /// Set the panel's content (text/image/etc) before or after calling this,
    /// depending on how your detail panel script is structured.
    /// </summary>
    public void OpenDetailPanel()
    {
        if (detailPanel != null)
            detailPanel.SetActive(true);
    }

    public void CloseDetailPanel()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    private void ShowOnlyPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == index);
        }
    }

    private void UpdateButtonStates()
    {
        if (prevButton != null)
            prevButton.interactable = currentPageIndex > 0;

        if (nextButton != null)
            nextButton.interactable = currentPageIndex < pages.Length - 1;
    }
}