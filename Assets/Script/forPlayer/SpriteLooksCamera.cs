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
        if (Camera.main == null) return;

        transform.forward = Camera.main.transform.forward;
    }
}