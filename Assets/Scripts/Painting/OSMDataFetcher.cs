using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

public class OSMDataFetcher : MonoBehaviour
{
    public string overpassAPI = "https://overpass-api.de/api/interpreter";
    public int zoom = 14;
    public TileManager tileManager;
    public Material roadMaterial; // Assign a simple material in the Inspector
    public RoadPainter roadPainter;

    public List<Vector3[]> storedRoadSegments = new List<Vector3[]>(); // Stores road segment positions
    public List<Vector3> roadPositions = new List<Vector3>();

    private HashSet<Vector2Int> loadedTiles = new HashSet<Vector2Int>(); // Keeps track of loaded tiles
    private Dictionary<Vector2Int, List<Vector3[]>> cachedRoads = new Dictionary<Vector2Int, List<Vector3[]>>();
    private Dictionary<Vector2Int, List<GameObject>> renderedRoads = new Dictionary<Vector2Int, List<GameObject>>();


    void Start()
    {
        if (tileManager == null)
        {
            tileManager = FindObjectOfType<TileManager>();  // Find TileLoader in the scene
            if (tileManager == null)
            {
                Debug.LogError("tileManager not found! Make sure it is instantiated and available.");
                return;
            }
        }
    }

    public void LoadRoadsForNewTile(Vector2Int tileCoords)
    {
        StartCoroutine(FetchRoadDataForTile(tileCoords));
    }

    IEnumerator FetchRoadDataForTile(Vector2Int tileCoords)
    {
        if (!cachedRoads.ContainsKey(tileCoords)) // Skip if already loaded
        {
            float minLat, minLon, maxLat, maxLon;
            ConvertTileToBoundingBox(tileCoords.x, tileCoords.y, tileManager.zoom, out minLat, out minLon, out maxLat, out maxLon);

            string query = $"[out:json];(way[\"highway\"~\"^(primary|secondary|tertiary|residential)$\"]({minLat},{minLon},{maxLat},{maxLon});node(w););out;";
            string url = $"{overpassAPI}?data={UnityWebRequest.EscapeURL(query)}";

            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonData = request.downloadHandler.text;
                List<Vector3[]> roadSegments = ParseRoadData(jsonData, tileCoords);
                cachedRoads[tileCoords] = roadSegments;
                storedRoadSegments.AddRange(roadSegments);
                UnloadRoads();
                DrawRoads();  // ✅ Refresh the roads
            }
            else
            {
                Debug.LogError($"Failed to fetch OSM data for tile {tileCoords}: {request.error}");
            }
        }
    }



    List<Vector3[]> ParseRoadData(string jsonData, Vector2Int tileCoords)
    {
        List<Vector3[]> roadSegments = new List<Vector3[]>();
        Dictionary<int, Vector3> nodePositions = new Dictionary<int, Vector3>();

        OSMResponse response = JsonUtility.FromJson<OSMResponse>(jsonData);

        // Store all nodes
        foreach (var element in response.elements)
        {
            if (element.type == "node")
            {
                Vector3 worldPos = ConvertLatLonToUnity(element.lat, element.lon, tileCoords.x, tileCoords.y);
                nodePositions[element.id] = worldPos;
            }
        }

        // Process ways using stored nodes
        foreach (var element in response.elements)
        {
            if (element.type == "way" && element.nodes.Count > 1)
            {
                for (int i = 0; i < element.nodes.Count - 1; i++)
                {
                    if (!nodePositions.ContainsKey(element.nodes[i]) || !nodePositions.ContainsKey(element.nodes[i + 1]))
                        continue;

                    Vector3 start = nodePositions[element.nodes[i]];
                    Vector3 end = nodePositions[element.nodes[i + 1]];
                    float distance = Vector3.Distance(start, end);

                    // **STEP 1: FILTER OUT TOO LONG SEGMENTS**
                    if (distance > 3f)
                    {
                        continue;
                    }

                    // **STEP 2: SPLIT REMAINING SEGMENTS INTO 0.05f PIECES**
                    float segmentLength = 0.2f;
                    int numSegments = Mathf.Max(1, Mathf.CeilToInt(distance / segmentLength));

                    Vector3 previousPoint = start;

                    for (int j = 1; j <= numSegments; j++) // Start from 1 to avoid duplicate start point
                    {
                        float t = j / (float)numSegments;
                        Vector3 interpolatedPoint = Vector3.Lerp(start, end, t);

                        // **Add each small segment as a separate road segment**
                        roadSegments.Add(new Vector3[] { previousPoint, interpolatedPoint });

                        previousPoint = interpolatedPoint;
                    }
                }
            }
        }

        Debug.Log($"Loaded {roadSegments.Count} road segments for tile {tileCoords}");
        return roadSegments;
    }

    void DrawRoads()
    {
        foreach (var tile in cachedRoads.Keys)
        {
            if (!cachedRoads.ContainsKey(tile)) continue; // Skip tiles with no roads

            List<GameObject> roadObjList = new List<GameObject>();
            foreach (Vector3[] segment in cachedRoads[tile])
            {
                GameObject roadObj = new GameObject($"RoadSegment {tile}");
                LineRenderer lineRenderer = roadObj.AddComponent<LineRenderer>();
                roadObj.AddComponent<RoadSegmentTrigger>();
                BoxCollider collider = roadObj.AddComponent<BoxCollider>();

                lineRenderer.positionCount = segment.Length;
                lineRenderer.SetPositions(segment);
                lineRenderer.startWidth = 0.03f;
                lineRenderer.endWidth = 0.03f;
                lineRenderer.material = roadMaterial;
                lineRenderer.useWorldSpace = true;  // Ensure world space positioning
                lineRenderer.sortingOrder = 10;  // Ensure it's drawn above the map

                float segmentLength = Vector3.Distance(segment[0], segment[1]);
                Vector3 midPoint = (segment[0] + segment[1]) / 2;

                collider.center = midPoint;
                collider.size = new Vector3(0.05f, 1f, segmentLength);
                collider.isTrigger = true;

                roadObjList.Add(roadObj);
            }
            if (roadObjList.Count > 0) {
                renderedRoads[tile] = roadObjList;
            }
        }
    }

    void UnloadRoads() {
        foreach (var tile in renderedRoads.Keys)
        {
            if (tile == new Vector2(tileManager.tileX, tileManager.tileY)) continue;

            foreach (GameObject roadObj in renderedRoads[tile])
            {
                Destroy(roadObj);
            }
            cachedRoads.Remove(tile);
        }
    }

    void ConvertTileToBoundingBox(int tileX, int tileY, int zoom, out float minLat, out float minLon, out float maxLat, out float maxLon)
    {
        float numTiles = Mathf.Pow(2, zoom);
        minLon = tileX / numTiles * 360f - 180f;
        maxLon = (tileX + 1) / numTiles * 360f - 180f;

        // Cast lat and lon to double before passing to Math.Sinh
        minLat = Mathf.Atan((float)Math.Sinh(Math.PI * (1 - 2 * (tileY + 1) / numTiles))) * Mathf.Rad2Deg;
        maxLat = Mathf.Atan((float)Math.Sinh(Math.PI * (1 - 2 * tileY / numTiles))) * Mathf.Rad2Deg;
    }

    Vector3 ConvertLatLonToUnity(float lat, float lon, int tileX, int tileY)
    {
        int zoom = tileManager.zoom;
        float numTiles = Mathf.Pow(2, zoom);

        float tileFloatX = (lon + 180f) / 360f * numTiles;
        float tileFloatY = (1f - Mathf.Log(Mathf.Tan(lat * Mathf.PI / 180f) + 1f / Mathf.Cos(lat * Mathf.PI / 180f)) / Mathf.PI) / 2f * numTiles;

        // Convert global tile position to local tile position
        float localX = (tileFloatX - tileX) * tileManager.tileSize;
        float localY = (tileFloatY - tileY) * tileManager.tileSize;
        localY = -localY;  // Flip Y to match Unity's coordinate system


        // Offset world position based on tile index
        float worldX = (tileX - tileManager.tileX) * tileManager.tileSize - tileManager.tileSize/2 + localX;
        float worldY = (tileManager.tileY - tileY) * tileManager.tileSize + tileManager.tileSize/2 + localY;

        Vector3 worldPos = new Vector3(worldX, 0.1f, worldY);
        return worldPos;
    }





    [System.Serializable]
    public class OSMResponse
    {
        public List<OSMElement> elements;
    }

    [System.Serializable]
    public class OSMElement
    {
        public string type;
        public int id;
        public List<int> nodes; // Only for ways
        public float lat; // Only for nodes
        public float lon; // Only for nodes
    }

    [System.Serializable]
    public class OSMNode
    {
        public float lat;
        public float lon;
    }
}
