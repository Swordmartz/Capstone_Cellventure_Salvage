using UnityEngine;
using UnityEngine.UI;

public class AlmanacButton : MonoBehaviour
{
    [Header("Manager")]
    public AlmanacManager almanacManager;

    [Header("Main info panel this button should show")]
    public GameObject mainInfoPanelToShow;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnAlmanacButtonPressed);
    }

    private void OnAlmanacButtonPressed()
    {
        if (almanacManager != null && mainInfoPanelToShow != null)
        {
            almanacManager.ActivateCell(mainInfoPanelToShow);
        }
    }
}