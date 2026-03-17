using UnityEngine;
using System.Collections;

public class HeartFrameAnimator : MonoBehaviour
{
    [Header("Frames")]
    public GameObject[] heartPrefabs;
    public float frameRate = 10f;

    private int currentFrame = 0;
    private GameObject[] pooledFrames;

    private Vector3 hiddenPos = new Vector3(9999, 9999, 9999); // offscreen

    void Start()
    {
        if (heartPrefabs.Length == 0)
        {
            Debug.LogWarning("No heart prefabs assigned!");
            return;
        }

        // Preload all prefabs once
        pooledFrames = new GameObject[heartPrefabs.Length];
        for (int i = 0; i < heartPrefabs.Length; i++)
        {
            pooledFrames[i] = Instantiate(heartPrefabs[i], transform);
            pooledFrames[i].transform.localPosition = hiddenPos; // keep hidden
        }

        // Show first frame
        pooledFrames[0].transform.localPosition = Vector3.zero;

        // Start coroutine
        StartCoroutine(AnimateFrames());
    }

    IEnumerator AnimateFrames()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f / frameRate);

            // Hide current by moving offscreen (no SetActive)
            pooledFrames[currentFrame].transform.localPosition = hiddenPos;

            // Next frame
            currentFrame = (currentFrame + 1) % pooledFrames.Length;

            // Show next
            pooledFrames[currentFrame].transform.localPosition = Vector3.zero;
        }
    }
}
