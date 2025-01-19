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

    public List<Vector3> roadPositions = new List<Vector3>(); // Stores road positions

    void Start()
    {
        StartCoroutine(FetchRoadData());
    }

    IEnumerator FetchRoadData()
    {
        // Convert current tile bounds to lat/lon
        float minLat, minLon, maxLat, maxLon;
        ConvertTileToBoundingBox(tileLoader.tileX, tileLoader.tileY, zoom, out minLat, out minLon, out maxLat, out maxLon);

        string query = $"[out:json];way[\"highway\"]({minLat},{minLon},{maxLat},{maxLon});out;";
        string url = $"{overpassAPI}?data={UnityWebRequest.EscapeURL(query)}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch OSM data: " + request.error);
        }
        else
        {
            ParseRoadData(request.downloadHandler.text);
        }
    }

    void ParseRoadData(string jsonData)
    {
        roadPositions.Clear();

        OSMResponse response = JsonUtility.FromJson<OSMResponse>(jsonData);

        foreach (var element in response.elements)
        {
            if (element.type == "way")
            {
                foreach (var node in element.nodes)
                {
                    Vector3 worldPos = ConvertLatLonToUnity(node.lat, node.lon);
                    roadPositions.Add(worldPos);
                }
            }
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
        public List<OSMNode> nodes;
    }

    [System.Serializable]
    public class OSMNode
    {
        public float lat;
        public float lon;
    }
}
