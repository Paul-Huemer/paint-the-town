using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class RoadPainter : MonoBehaviour
{
    public Dictionary<Vector2Int, List<Vector3[]>> cachedRoads = new Dictionary<Vector2Int, List<Vector3[]>>();
    private Dictionary<Vector2Int, List<GameObject>> renderedRoads = new Dictionary<Vector2Int, List<GameObject>>();

    public TileManager tileManager;
    public Material roadMaterial;
    public SavingManager savingManager;

    public void DrawRoads()
    {
        foreach (var tile in cachedRoads.Keys)
        {
            if (!cachedRoads.ContainsKey(tile)) continue; // Skip tiles with no roads

            List<GameObject> roadObjList = new List<GameObject>();
            foreach (Vector3[] segment in cachedRoads[tile])
            {
                GameObject roadObj = new GameObject($"RoadSegment {tile} {segment[0].x + segment[0].y + segment[0].z + segment[1].x + segment[1].y + segment[1].z}");
                LineRenderer lineRenderer = roadObj.AddComponent<LineRenderer>();
                RoadSegmentTrigger roadSegmentTrigger = roadObj.AddComponent<RoadSegmentTrigger>();
                roadSegmentTrigger.savingManager = savingManager;
                roadSegmentTrigger.tile = tile;
                BoxCollider collider = roadObj.AddComponent<BoxCollider>();

                lineRenderer.positionCount = segment.Length;
                lineRenderer.SetPositions(segment);
                lineRenderer.startWidth = 0.05f;
                lineRenderer.endWidth = 0.05f;
                lineRenderer.material = roadMaterial;
                lineRenderer.useWorldSpace = true;
                lineRenderer.sortingOrder = 10;

                float segmentLength = Vector3.Distance(segment[0], segment[1]);
                Vector3 midPoint = (segment[0] + segment[1]) / 2;

                collider.center = midPoint;
                collider.size = new Vector3(0.05f, 1f, segmentLength);
                collider.isTrigger = true;

                roadObjList.Add(roadObj);

                if (savingManager.walkedOnRoads.ContainsKey(tile) && savingManager.walkedOnRoads[tile].Contains(roadObj.name))
                {
                    Debug.Log("yes");
                    lineRenderer.enabled = true;
                } else {
                    lineRenderer.enabled = false;
                }
            }
            if (roadObjList.Count > 0)
            {
                renderedRoads[tile] = roadObjList;
            }
        }
    }

    public void UnloadRoads()
    {
        foreach (var tile in renderedRoads.Keys)
        {
            if (tile == new Vector2(tileManager.tileX, tileManager.tileY)) continue;

            foreach (GameObject roadObj in renderedRoads[tile])
            {
                if (roadObj == null) continue;
                LineRenderer lineRenderer = roadObj.GetComponent<LineRenderer>();
                BoxCollider collider = roadObj.GetComponent<BoxCollider>();
                if (lineRenderer == null) continue;

                Destroy(roadObj);
            }
            cachedRoads.Remove(tile);
        }
    }
}
