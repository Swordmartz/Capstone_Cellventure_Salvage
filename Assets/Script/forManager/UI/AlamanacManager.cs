using UnityEngine;

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

    private void Start()
    {
        HideAllMainInfo();
        HideAllTelescopeInfo();

        if (entries.Length > 0 && entries[0].mainInfoPanel != null)
        {
            ActivateCell(entries[0].mainInfoPanel);
        }
    }

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