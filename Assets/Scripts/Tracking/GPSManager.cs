using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GPSManager : MonoBehaviour
{
    public TileLoader tileLoader;
    public PlayerMarker playerMarker;
    public Transform mapTransform;

    private bool gpsInitialized = false;
    private float updateInterval = 1.0f; // Update GPS every second


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

        Input.location.Start(0.1f, 0.1f);

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

        Debug.Log("GPS initialized successfully.");
        gpsInitialized = true;

        StartCoroutine(UpdateGPS());
    }

    IEnumerator UpdateGPS()
    {
        while (gpsInitialized)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                float lat = Input.location.lastData.latitude;
                float lon = Input.location.lastData.longitude;
                double timestamp = Input.location.lastData.timestamp;

                Debug.Log($"Updated GPS: lat: {lat}, long: {lon}, timestamp: {timestamp}");

                int tileX, tileY;
                ConvertLatLonToTile(lat, lon, tileLoader.zoom, out tileX, out tileY);

                if (tileX != tileLoader.tileX || tileY != tileLoader.tileY)
                {
                    tileLoader.tileX = tileX;
                    tileLoader.tileY = tileY;
                    tileLoader.StartCoroutine(tileLoader.LoadTile(tileX, tileY, tileLoader.zoom));
                }

                Vector3 targetPosition = ConvertGPSPositionToMap(lat, lon);

                // Smoothly move the player marker to the new position
                playerMarker.UpdatePosition(targetPosition);
            }
            else
            {
                Debug.LogWarning("GPS lost signal. Waiting for recovery...");
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }

    public void ConvertLatLonToTile(float lat, float lon, int zoom, out int tileX, out int tileY)
    {
        tileX = (int)((lon + 180.0) / 360.0 * (1 << zoom));
        tileY = (int)((1f - Mathf.Log(Mathf.Tan(lat * Mathf.PI / 180f) + 1f / Mathf.Cos(lat * Mathf.PI / 180f)) / Mathf.PI) / 2f * (1 << zoom));
    }

    Vector3 ConvertGPSPositionToMap(float lat, float lon)
    {
        int zoom = tileLoader.zoom;
        int tileX, tileY;

        ConvertLatLonToTile(lat, lon, zoom, out tileX, out tileY);

        float numTiles = Mathf.Pow(2, zoom);
        float tileFloatX = (lon + 180f) / 360f * numTiles;
        float tileFloatY = (1f - Mathf.Log(Mathf.Tan(lat * Mathf.PI / 180f) + 1f / Mathf.Cos(lat * Mathf.PI / 180f)) / Mathf.PI) / 2f * numTiles;

        float localX = tileFloatX - Mathf.Floor(tileFloatX);
        float localY = tileFloatY - Mathf.Floor(tileFloatY);
        localY = 1 - localY;

        Debug.Log($"GPS lat: {lat}, lon: {lon} | Tile: {tileX}, {tileY} | Local X,Y: {localX}, {localY}");

        float mapWidth = 10f;
        float mapHeight = 10f;
        float worldX = (localX - 0.5f) * mapWidth;
        float worldY = (localY - 0.5f) * mapHeight;

        return new Vector3(worldX, 0.1f, worldY);
    }

    private void OnApplicationQuit()
    {
        Input.location.Stop();
        Input.compass.enabled = false;
        Input.gyro.enabled = false;
    }
}
