using UnityEngine;

public class SpriteLooksCamera : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }
    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.forward);
    }
}