using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

public class OSMDataFetcher : MonoBehaviour
{
    public string overpassAPI = "https://overpass-api.de/api/interpreter";
    public int zoom = 14;
    public TileLoader tileLoader;
    public Material roadMaterial; // Assign a simple material in the Inspector

    private List<Vector3[]> roadSegments = new List<Vector3[]>(); // Stores road segment positions
    public List<Vector3> roadPositions = new List<Vector3>();

    void Start()
    {
        StartCoroutine(FetchRoadData());
    }

    IEnumerator FetchRoadData()
    {
        float minLat, minLon, maxLat, maxLon;
        ConvertTileToBoundingBox(tileLoader.tileX, tileLoader.tileY, zoom, out minLat, out minLon, out maxLat, out maxLon);

        string query = $"[out:json];(way[\"highway\"~\"^(primary|secondary|tertiary|residential)$\"]({minLat},{minLon},{maxLat},{maxLon});node(w););out;";

        string url = $"{overpassAPI}?data={UnityWebRequest.EscapeURL(query)}";

        Debug.Log($"Fetching OSM Data from: {url}");  // ✅ Log URL for testing

        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch OSM data: " + request.error);
        }
        else
        {
            string jsonData = request.downloadHandler.text;
            Debug.Log($"OSM Response: {jsonData}");  // ✅ Print JSON response

            ParseRoadData(jsonData);
            DrawRoads();
        }
    }

    void ParseRoadData(string jsonData)
    {
        roadSegments.Clear();
        Dictionary<int, Vector3> nodePositions = new Dictionary<int, Vector3>();

        OSMResponse response = JsonUtility.FromJson<OSMResponse>(jsonData);

        // Store all nodes
        foreach (var element in response.elements)
        {
            if (element.type == "node")
            {
                Vector3 worldPos = ConvertLatLonToUnity(element.lat, element.lon);
                nodePositions[element.id] = worldPos;
            }
        }

        // Process ways using the stored nodes
        foreach (var element in response.elements)
        {
            if (element.type == "way" && element.nodes.Count > 1)
            {
                List<Vector3> roadPoints = new List<Vector3>();
                Vector3 lastPoint = Vector3.zero;

                foreach (var nodeId in element.nodes)
                {
                    if (nodePositions.ContainsKey(nodeId))
                    {
                        Vector3 currentPoint = nodePositions[nodeId];

                        // If this is not the first point, check the distance
                        if (lastPoint != Vector3.zero)
                        {
                            float distance = Vector3.Distance(lastPoint, currentPoint);

                            // Set a threshold distance (e.g., 50 units)
                            if (distance > 3f) // Replace 50f with your desired distance threshold
                            {
                                Debug.Log($"Skipping point at {currentPoint} because it's too far from {lastPoint}. Distance: {distance}");
                                continue; // Skip this point if it's too far
                            }
                        }

                        roadPoints.Add(currentPoint); // Add the point to the road segment
                        lastPoint = currentPoint; // Update the last point
                    }
                    else
                    {
                        Debug.LogWarning($"Node ID {nodeId} missing lat/lon data! Skipping...");
                    }
                }

                if (roadPoints.Count > 1)
                {
                    // Debug each segment
                    Debug.Log($"RoadSegment: {roadPoints.Count} points, Start: {roadPoints[0]}, End: {roadPoints[roadPoints.Count - 1]}");
                    roadSegments.Add(roadPoints.ToArray());
                }
            }
        }

        Debug.Log($"Loaded {roadSegments.Count} road segments.");
    }

    void DrawRoads()
    {
        int index = 0;
        foreach (Vector3[] road in roadSegments)
        {
            GameObject roadObj = new GameObject("RoadSegment " + index);
            LineRenderer lineRenderer = roadObj.AddComponent<LineRenderer>();

            string roadLog = "Road Segment " + index +": ";
            foreach (var point in road)
            {
                roadLog += $"({point.x}, {point.y}, {point.z}), ";
            }
            Debug.Log(roadLog);

            lineRenderer.positionCount = road.Length;
            lineRenderer.SetPositions(road);
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.material = roadMaterial;

            index += 1;
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

    Vector3 ConvertLatLonToUnity(float lat, float lon)
    {
        int zoom = tileLoader.zoom;
        int tileX, tileY;

        tileLoader.ConvertLatLonToTile(lat, lon, zoom, out tileX, out tileY);

        float numTiles = Mathf.Pow(2, zoom);
        float tileFloatX = (lon + 180f) / 360f * numTiles;
        float tileFloatY = (1f - Mathf.Log(Mathf.Tan(lat * Mathf.PI / 180f) + 1f / Mathf.Cos(lat * Mathf.PI / 180f)) / Mathf.PI) / 2f * numTiles;

        float localX = tileFloatX - Mathf.Floor(tileFloatX);
        float localY = tileFloatY - Mathf.Floor(tileFloatY);
        localY = 1 - localY;

        //Debug.Log($"GPS lat: {lat}, lon: {lon} | Tile: {tileX}, {tileY} | Local X,Y: {localX}, {localY}");

        float mapWidth = 10f;
        float mapHeight = 10f;
        float worldX = (localX - 0.5f) * mapWidth;
        float worldY = (localY - 0.5f) * mapHeight;

        return new Vector3(worldX, 0.1f, worldY);
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
