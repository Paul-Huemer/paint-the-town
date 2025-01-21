using UnityEngine;

public class RoadSegmentTrigger : MonoBehaviour
{
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false; // Hide by default
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))  // Ensure the player has the "Player" tag
        {
            lineRenderer.enabled = true;  // Show the segment when the player enters
        }
    }
}
