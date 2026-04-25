using UnityEngine;

public class EnlargeMap : MonoBehaviour
{
    public GameObject objectToDisable;
    public GameObject objectToEnable;

    public void SwitchObjects()
    {
        if (objectToDisable != null)
            objectToDisable.SetActive(false);

        if (objectToEnable != null)
            objectToEnable.SetActive(true);
    }
}