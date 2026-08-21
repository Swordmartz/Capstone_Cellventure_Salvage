using UnityEngine;
using UnityEngine.UI;

public class AlmanacManager : MonoBehaviour
{
    [System.Serializable]
    public class AlmanacEntry
    {
        [Header("Cell Info")]
        public string cellName;

        [Tooltip("Normal info shown when clicking the cell button.")]
        public GameObject mainInfoPanel;

        [Tooltip("Extra info shown when clicking the telescope.")]
        public GameObject telescopeInfoPanel;
    }

    [Header("All Almanac Entries")]
    public AlmanacEntry[] entries;

    private AlmanacEntry currentEntry;

    // ---------------------------------------------------------------
    // PAGE NAVIGATION
    // ---------------------------------------------------------------
    [Header("Page Navigation")]
    [Tooltip("Pages shown in the '1st slot'. Index 0 is the starting page. " +
             "Pressing Next moves forward through this list, Prev moves back.")]
    public GameObject[] firstSlotPages;

    [Tooltip("The '2nd slot' / detail pages, one per entry in firstSlotPages (matched by index). " +
             "Whichever one corresponds to the current page is shown; all others are hidden.")]
    public GameObject[] secondSlotPages;

    [Tooltip("Optional - will be auto-disabled/enabled as pages change.")]
    public Button nextButton;

    [Tooltip("Optional - will be auto-disabled/enabled as pages change.")]
    public Button prevButton;

    private int currentPageIndex = 0;

    private void Start()
    {
        HideAllMainInfo();
        HideAllTelescopeInfo();

        if (entries.Length > 0 && entries[0].mainInfoPanel != null)
        {
            ActivateCell(entries[0].mainInfoPanel);
        }

        SetupPages();
    }

    private void SetupPages()
    {
        currentPageIndex = 0;

        if (firstSlotPages == null || firstSlotPages.Length == 0)
            return;

        ShowFirstSlotPage(currentPageIndex);
        UpdateSecondSlotPage();

        if (prevButton != null)
            prevButton.interactable = false;

        if (nextButton != null)
            nextButton.interactable = firstSlotPages.Length > 1;
    }

    public void NextPage()
    {
        if (firstSlotPages == null || firstSlotPages.Length == 0)
            return;

        if (currentPageIndex >= firstSlotPages.Length - 1)
            return; // already on the last page

        currentPageIndex++;
        ShowFirstSlotPage(currentPageIndex);

        UpdateSecondSlotPage();

        if (prevButton != null)
            prevButton.interactable = true;

        if (nextButton != null)
            nextButton.interactable = currentPageIndex < firstSlotPages.Length - 1;
    }

    public void PrevPage()
    {
        if (firstSlotPages == null || firstSlotPages.Length == 0)
            return;

        if (currentPageIndex <= 0)
            return; // already at the first page

        currentPageIndex--;
        ShowFirstSlotPage(currentPageIndex);

        UpdateSecondSlotPage();

        if (nextButton != null)
            nextButton.interactable = true;

        if (currentPageIndex <= 0 && prevButton != null)
            prevButton.interactable = false;
    }

    private void ShowFirstSlotPage(int index)
    {
        for (int i = 0; i < firstSlotPages.Length; i++)
        {
            if (firstSlotPages[i] != null)
                firstSlotPages[i].SetActive(i == index);
        }
    }

    // Shows the secondSlotPages entry that matches the current page index and
    // hides all the others (mirrors ShowFirstSlotPage).
    private void UpdateSecondSlotPage()
    {
        if (secondSlotPages == null)
            return;

        for (int i = 0; i < secondSlotPages.Length; i++)
        {
            if (secondSlotPages[i] != null)
                secondSlotPages[i].SetActive(i == currentPageIndex);
        }
    }

    // ---------------------------------------------------------------
    // EXISTING CELL / TELESCOPE LOGIC (unchanged)
    // ---------------------------------------------------------------

    public void ActivateCell(GameObject mainInfoPanelToShow)
    {
        if (mainInfoPanelToShow == null)
        {
            Debug.LogWarning("Main info panel to show is null.");
            return;
        }

        HideAllMainInfo();
        HideAllTelescopeInfo();

        currentEntry = null;

        foreach (AlmanacEntry entry in entries)
        {
            if (entry.mainInfoPanel == mainInfoPanelToShow)
            {
                currentEntry = entry;
                entry.mainInfoPanel.SetActive(true);

                Debug.Log("Selected cell: " + entry.cellName);
                return;
            }
        }

        Debug.LogWarning("No AlmanacEntry found for: " + mainInfoPanelToShow.name);
    }

    public void ShowTelescopeInfo()
    {
        if (currentEntry == null)
        {
            Debug.LogWarning("No current cell selected.");
            return;
        }

        HideAllTelescopeInfo();

        if (currentEntry.telescopeInfoPanel != null)
        {
            currentEntry.telescopeInfoPanel.SetActive(true);
            Debug.Log("Showing telescope info for: " + currentEntry.cellName);
        }
        else
        {
            Debug.LogWarning("No telescope info assigned for: " + currentEntry.cellName);
        }
    }

    public void HideTelescopeInfo()
    {
        HideAllTelescopeInfo();
    }

    private void HideAllMainInfo()
    {
        foreach (AlmanacEntry entry in entries)
        {
            if (entry.mainInfoPanel != null)
                entry.mainInfoPanel.SetActive(false);
        }
    }

    private void HideAllTelescopeInfo()
    {
        foreach (AlmanacEntry entry in entries)
        {
            if (entry.telescopeInfoPanel != null)
                entry.telescopeInfoPanel.SetActive(false);
        }
    }
}