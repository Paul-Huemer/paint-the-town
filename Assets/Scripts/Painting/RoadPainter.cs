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
        roadSegments = osmDataFetcher.storedRoadSegments; // roadSegments from OSM data

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
        if (!isInitialized) return; // If not initialized, do nothing

        Vector3 playerPos = playerMarker.transform.position;
        Vector3 nearestRoadPoint = Vector3.zero;
        float minDistance = detectionRadius;  // Max distance within which the snap happens
        bool foundRoad = false;

        // Go through each road segment and find the closest road point
        foreach (Vector3[] roadSegment in osmDataFetcher.storedRoadSegments)
        {
            foreach (Vector3 roadPosition in roadSegment)
            {
                float distance = Vector3.Distance(playerPos, roadPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestRoadPoint = roadPosition;
                    foundRoad = true;
                }
            }
        }

        // Only paint if a road was found within detection radius
        if (foundRoad && !paintedRoads.Contains(nearestRoadPoint))
        {
            paintedRoads.Add(nearestRoadPoint);
            PaintRoad(nearestRoadPoint);
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
