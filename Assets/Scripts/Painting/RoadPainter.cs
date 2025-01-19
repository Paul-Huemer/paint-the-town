using UnityEngine;
using System.Collections.Generic;

public class RoadPainter : MonoBehaviour
{
    public PlayerMarker playerMarker;
    public OSMDataFetcher osmDataFetcher;
    public Color teamColor = Color.magenta;
    private HashSet<Vector3> paintedRoads = new HashSet<Vector3>();
    public float detectionRadius = 0.5f;

    void Update()
    {
        Vector3 playerPos = playerMarker.transform.position;

        foreach (Vector3 road in osmDataFetcher.roadPositions)
        {
            if (Vector3.Distance(playerPos, road) < detectionRadius)
            {
                if (!paintedRoads.Contains(road))
                {
                    paintedRoads.Add(road);
                    PaintRoad(road);
                }
            }
        }
    }

    void PaintRoad(Vector3 position)
    {
        GameObject roadSegment = GameObject.CreatePrimitive(PrimitiveType.Quad);
        roadSegment.transform.position = position;
        roadSegment.transform.localScale = new Vector3(1, 0.05f, 1);
        roadSegment.GetComponent<Renderer>().material.color = teamColor;
    }
}
