using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private Transform teleportTarget;

    public void Execute()
    {
        // Example behavior: hide the item when picked up
        if (teleportTarget != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            player.transform.position = teleportTarget.position;
            Debug.Log("Player teleported to " + teleportTarget.name);
        }
    }
}
