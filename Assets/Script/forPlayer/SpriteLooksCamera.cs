using UnityEngine;

public class SpriteLooksCamera : MonoBehaviour
{
    public Camera targetCamera;

    void Start()
    {
        // Fallback to main camera if none assigned
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;
        transform.LookAt(transform.position + targetCamera.transform.forward);
    }
}