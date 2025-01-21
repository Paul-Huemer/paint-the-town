using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoadPainter : MonoBehaviour
{
    public PlayerMarker playerMarker;
    public OSMDataFetcher osmDataFetcher;
    public Color teamColor = Color.magenta;
    private HashSet<Vector3> paintedRoads = new HashSet<Vector3>();
    public float detectionRadius = 0.2f; // The distance within which the road is painted
    private List<Vector3[]> roadSegments;

    private LineRenderer lineRenderer;  // LineRenderer to draw the painted path
    private bool isInitialized = false; // Flag to check if initialization is complete

    public float initializationDelay = 3f; // Delay before starting the painting process

    void Start()
    {
        // Initialize the LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startColor = teamColor;
        lineRenderer.endColor = teamColor;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Assuming osmDataFetcher will provide the road segments
        roadSegments = osmDataFetcher.roadSegments; // roadSegments from OSM data

        // Start the initialization delay
        StartCoroutine(InitializePainting());
    }

    IEnumerator InitializePainting()
    {
        // Wait for the specified delay before starting
        yield return new WaitForSeconds(initializationDelay);

        isInitialized = true; // Set initialization flag to true
    }

    void Update()
    {
        if (!isInitialized)
            return; // If not initialized, do nothing

        Vector3 playerPos = playerMarker.transform.position;

        // Go through each road segment and check if the player is close enough
        foreach (Vector3[] roadSegment in roadSegments)
        {
            foreach (Vector3 roadPosition in roadSegment)
            {
                // If the player is close enough to a road position, paint the road
                if (Vector3.Distance(playerPos, roadPosition) < detectionRadius)
                {
                    // Only paint if it hasn't been painted before
                    if (!paintedRoads.Contains(roadPosition))
                    {
                        paintedRoads.Add(roadPosition);
                        PaintRoad(roadPosition);
                    }
                }
            }
        }
    }

    void PaintRoad(Vector3 position)
    {
        // Add the road position to the LineRenderer as a new point
        int currentPositionCount = lineRenderer.positionCount;
        lineRenderer.positionCount = currentPositionCount + 1;  // Increase the position count for the new point
        lineRenderer.SetPosition(currentPositionCount, position); // Set the new position
    }
}
