using UnityEngine;
using UnityEngine.UI;

public class TelescopeButton : MonoBehaviour
{
    [Header("Manager")]
    public AlmanacManager almanacManager;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnTelescopePressed);
    }

    private void OnTelescopePressed()
    {
        if (almanacManager != null)
        {
            almanacManager.ShowTelescopeInfo();
        }
    }
}