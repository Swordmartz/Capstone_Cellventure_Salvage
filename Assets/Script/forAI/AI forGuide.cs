using UnityEngine;

public class AIforGuide : MonoBehaviour
{
    [Header("Guide System")]
    public Transform guideMark;     // Current location to guide to
    public Transform player;        // Player transform
    public LineRenderer guideLine;  // Line renderer for dotted trail
    public LayerMask obstacleMask;  // Layer mask for walls or obstacles
    public float minDistance = 1f;  // Hide line if player is too close
    public float maxDistance = 10f; // Show line if player far enough
    public bool guideEnabled = true; // Can be toggled off


    // ------------------ Unity Callbacks ------------------
    void Start()
    {
        if (guideLine != null)
            guideLine.enabled = false;
    }
    private void Update()
    {
        UpdateGuideLine();
    }



    // ------------------ Guide Line Logic ------------------
    public void UpdateGuideLine()
    {
        if (!guideEnabled || guideMark == null || player == null || guideLine == null)
        {
            if (guideLine != null) guideLine.enabled = false;
            return;
        }

        float distance = Vector3.Distance(player.position, guideMark.position);
        bool blocked = Physics.Linecast(player.position, guideMark.position, obstacleMask);

        if (!blocked && distance >= minDistance && distance <= maxDistance)
        {
            guideLine.enabled = true;
            guideLine.positionCount = 2;
            guideLine.SetPosition(0, player.position);
            guideLine.SetPosition(1, guideMark.position);
        }
        else
        {
            guideLine.enabled = false;
        }
    }


}