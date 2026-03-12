using UnityEngine;

public class HeartFrameAnimator : MonoBehaviour
{
    [Header("Frames")]
    public GameObject[] heartFrames;   // Assign all heartbeat prefabs here
    public float frameRate = 10f;      // Frames per second

    private int currentFrame = 0;
    private float timer = 0f;
    private GameObject activeFrame;

    void Start()
    {
        if (heartFrames.Length == 0)
        {
            Debug.LogWarning("No heart frames assigned!");
            return;
        }

        SpawnFrame(currentFrame);
    }

    void Update()
    {
        if (heartFrames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer = 0f;

            // Remove previous frame
            if (activeFrame != null)
                Destroy(activeFrame);

            // Next frame
            currentFrame = (currentFrame + 1) % heartFrames.Length;
            SpawnFrame(currentFrame);
        }
    }

    void SpawnFrame(int index)
    {
        // Instantiate prefab as child of this GameObject
        activeFrame = Instantiate(
            heartFrames[index],
            transform.position,
            Quaternion.identity,
            transform
        );

        // Reset local position & rotation for proper alignment
        activeFrame.transform.localPosition = Vector3.zero;
        activeFrame.transform.localRotation = Quaternion.identity;
        activeFrame.transform.localScale = Vector3.one; // make sure scale is correct
    }
}