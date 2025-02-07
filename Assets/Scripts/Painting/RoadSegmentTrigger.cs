using UnityEngine;

public class RoadSegmentTrigger : MonoBehaviour
{

    private LineRenderer lineRenderer;
    public SavingManager savingManager;
    public Vector2Int tile;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        //lineRenderer.enabled = false; // Hide by default
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))  // Ensure the player has the "Player" tag
        {
            if (lineRenderer.enabled == false && savingManager != null && (!savingManager.walkedOnRoads.ContainsKey(tile) || !savingManager.walkedOnRoads[tile].Contains(gameObject.name))) {
                savingManager.saveWalkedOnRoad(gameObject, tile);
            }
            lineRenderer.enabled = true;  // Show the segment when the player enters
        }
    }
}