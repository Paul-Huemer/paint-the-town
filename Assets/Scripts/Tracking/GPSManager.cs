using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GPSManager : MonoBehaviour
{
    public TileLoader tileLoader;
    public PlayerMarker playerMarker; // Assign PlayerMarker in Inspector
    public Transform mapTransform; // Assign MapContainer in Inspector
    public Text debugText;

    private bool gpsInitialized = false;

    void Start()
    {
        StartCoroutine(StartGPS());
    }

    IEnumerator StartGPS()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("GPS is disabled. Enable location services.");
            yield break;
        }

        Input.location.Start();

        int maxWait = 10;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Failed to get GPS location.");
            yield break;
        }

        Debug.Log("GPS location success");
        gpsInitialized = true;
    }

    void Update()
    {
        if (!gpsInitialized || Input.location.status != LocationServiceStatus.Running)
            return;

        float lat = Input.location.lastData.latitude;
        float lon = Input.location.lastData.longitude;

        Debug.Log("lat: " + lat + ", long: " + lon);

        int tileX, tileY;
        ConvertLatLonToTile(lat, lon, tileLoader.zoom, out tileX, out tileY);

        if (tileX != tileLoader.tileX || tileY != tileLoader.tileY)
        {
            tileLoader.tileX = tileX;
            tileLoader.tileY = tileY;
            tileLoader.StartCoroutine(tileLoader.LoadTile(tileX, tileY, tileLoader.zoom));
        }

        Vector3 playerPosition = ConvertGPSPositionToMap(lat, lon); // Adjust to correct coordinate mapping
        debugText.text = "x: " + playerPosition.x + ", z: " + playerPosition.z;
        playerMarker.UpdatePosition(playerPosition);
    }


    void ConvertLatLonToTile(float lat, float lon, int zoom, out int tileX, out int tileY)
    {
        tileX = (int)((lon + 180.0) / 360.0 * (1 << zoom));
        tileY = (int)((1f - Mathf.Log(Mathf.Tan((float)lat * Mathf.PI / 180f) + 1f / Mathf.Cos((float)lat * Mathf.PI / 180f)) / Mathf.PI) / 2f * (1 << zoom));

    }

    Vector3 ConvertGPSPositionToMap(float lat, float lon)
    {
        int zoom = tileLoader.zoom;
        int tileX, tileY;

        ConvertLatLonToTile(lat, lon, zoom, out tileX, out tileY);

        // Total number of tiles at current zoom level
        float numTiles = Mathf.Pow(2, zoom);

        // Convert lat/lon to tile coordinates (floating-point values)
        float tileFloatX = (lon + 180f) / 360f * numTiles;
        float tileFloatY = (1f - Mathf.Log(Mathf.Tan(lat * Mathf.PI / 180f) + 1f / Mathf.Cos(lat * Mathf.PI / 180f)) / Mathf.PI) / 2f * numTiles;

        // Get precise position inside the tile (normalized 0-1 range)
        float localX = tileFloatX - Mathf.Floor(tileFloatX);
        float localY = tileFloatY - Mathf.Floor(tileFloatY);

        // Flip Y (since Unity’s coordinate system is bottom-left, OSM is top-left)
        localY = 1 - localY;

        Debug.Log($"GPS lat: {lat}, lon: {lon} | Tile: {tileX}, {tileY} | Local X,Y: {localX}, {localY}");

        // Scale to Unity world coordinates (assuming a 10x10 map)
        float mapWidth = 10f;
        float mapHeight = 10f;

        float worldX = (localX - 0.5f) * mapWidth;
        float worldY = (localY - 0.5f) * mapHeight;

        return new Vector3(worldX, 0.1f, worldY);
    }





    private void OnApplicationQuit()
    {
        Input.location.Stop();
    }
}
