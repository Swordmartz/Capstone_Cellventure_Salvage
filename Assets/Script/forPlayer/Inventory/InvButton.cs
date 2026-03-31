using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class InventoryUIButton : MonoBehaviour
{
    public Inventory playerInventory;
    public TMP_Text descriptionText; // Text UI to show description
    public float displayTime = 3f;   // Time in seconds before fading
    public float fadeDuration = 1f;  // How long the fade lasts

    private Coroutine fadeCoroutine;

    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ShowDescription);
        descriptionText.alpha = 0f; // Start invisible
                                    
        
    }
    void ShowDescription()
    {
        if (playerInventory.HasItem)
        {
            // Set the text
            descriptionText.text = playerInventory.currentItem.description;

            // Make text fully visible immediately
            descriptionText.alpha = 1f;

            // Stop any ongoing fade coroutine
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            // Start fade out coroutine
            fadeCoroutine = StartCoroutine(FadeOutText());
        }
    }

    private IEnumerator FadeOutText()
    {
        // Wait for displayTime seconds before fading
        yield return new WaitForSeconds(displayTime);

        float startAlpha = descriptionText.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            descriptionText.alpha = Mathf.Lerp(startAlpha, 0f, time / fadeDuration);
            yield return null;
        }

        descriptionText.alpha = 0f; // Ensure fully invisible at the end
    }
}