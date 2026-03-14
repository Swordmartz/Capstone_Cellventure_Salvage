using UnityEngine;

public class HeartFrameAnimator : MonoBehaviour
{
    [Header("Frames")]
    public GameObject[] heartFrames;
    public float frameRate = 10f;

    private int currentFrame = 0;
    private float timer = 0f;

    private GameObject[] spawnedFrames;

    void Start()
    {
        if (heartFrames.Length == 0)
        {
            Debug.LogWarning("No heart frames assigned!");
            return;
        }

        spawnedFrames = new GameObject[heartFrames.Length];

        // Spawn all frames once
        for (int i = 0; i < heartFrames.Length; i++)
        {
            spawnedFrames[i] = Instantiate(
                heartFrames[i],
                transform.position,
                Quaternion.identity,
                transform
            );

            spawnedFrames[i].transform.localPosition = Vector3.zero;
            spawnedFrames[i].transform.localRotation = Quaternion.identity;
            spawnedFrames[i].transform.localScale = Vector3.one;

            spawnedFrames[i].SetActive(false); // disable initially
        }

        // Show first frame
        spawnedFrames[currentFrame].SetActive(true);
    }

    void Update()
    {
        if (spawnedFrames == null || spawnedFrames.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= 1f / frameRate)
        {
            timer = 0f;

            // Hide current frame
            spawnedFrames[currentFrame].SetActive(false);

            // Next frame
            currentFrame = (currentFrame + 1) % spawnedFrames.Length;

            // Show next frame
            spawnedFrames[currentFrame].SetActive(true);
        }
    }
}
