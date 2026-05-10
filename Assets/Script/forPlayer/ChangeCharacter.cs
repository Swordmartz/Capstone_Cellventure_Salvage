using UnityEngine;
using Unity.Cinemachine;

public class PlayerAppearanceSwapper : MonoBehaviour
{
    [Header("Character Objects")]
    public GameObject normalCharacter;
    public GameObject itemCharacter;

    [Header("Reference")]
    public Inventory inventory;
    public CinemachineCamera virtualCamera; // ✅ New Cinemachine 3.x class

    private bool hasItem = false;

    void Update()
    {
        CheckInventory();
    }

    private void CheckInventory()
    {
        bool currentlyHasItem = inventory.HasItem;

        if (currentlyHasItem != hasItem)
        {
            hasItem = currentlyHasItem;
            SwapCharacter(hasItem);
        }
    }

    private void SwapCharacter(bool playerHasItem)
    {
        if (playerHasItem)
        {
            itemCharacter.transform.position = normalCharacter.transform.position;
            itemCharacter.transform.rotation = normalCharacter.transform.rotation;

            normalCharacter.SetActive(false);
            itemCharacter.SetActive(true);

            // ✅ Cinemachine 3.x syntax
            virtualCamera.Follow = itemCharacter.transform;
            virtualCamera.LookAt = itemCharacter.transform;
        }
        else
        {
            normalCharacter.transform.position = itemCharacter.transform.position;
            normalCharacter.transform.rotation = itemCharacter.transform.rotation;

            itemCharacter.SetActive(false);
            normalCharacter.SetActive(true);

            virtualCamera.Follow = normalCharacter.transform;
            virtualCamera.LookAt = normalCharacter.transform;
        }
    }
}