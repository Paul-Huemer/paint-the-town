using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GPSManager : MonoBehaviour
{
    public TileManager tileManager;
    public PlayerMarker playerMarker;

    private bool gpsInitialized = false;
    private float updateInterval = 0.2f; // Update GPS every second

    public float lat = 0;
    public float lon = 0;

    void Start()
    {
        if (tileManager == null)
        {
            tileManager = FindObjectOfType<TileManager>();
            if (tileManager == null)
            {
                Debug.LogError("TileManager not found! Make sure it is instantiated and available.");
                return;
            }
        }
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
                lat = Input.location.lastData.latitude;
                lon = Input.location.lastData.longitude;

                int newTileX, newTileY;
                ConvertLatLonToTile(lat, lon, tileManager.zoom, out newTileX, out newTileY);

                // ✅ Only reload tiles if the tile position changed
                if (newTileX != tileManager.tileX || newTileY != tileManager.tileY)
                {
                    tileManager.tileX = newTileX;
                    tileManager.tileY = newTileY;
                    //Debug.Log($"X {newTileX} Y {newTileY}");
                    tileManager.LoadTilesAround(new Vector2Int(newTileX, newTileY));  // ✅ Reload adjacent tiles
                }

                Vector3 targetPosition = ConvertGPSPositionToMap(lat, lon);
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
        int zoom = tileManager.zoom;
        int tileX, tileY;

        tileManager.ConvertLatLonToTile(lat, lon, zoom, out tileX, out tileY);

        float numTiles = Mathf.Pow(2, zoom);
        float tileFloatX = (lon + 180f) / 360f * numTiles;
        float tileFloatY = (1f - Mathf.Log(Mathf.Tan(lat * Mathf.PI / 180f) + 1f / Mathf.Cos(lat * Mathf.PI / 180f)) / Mathf.PI) / 2f * numTiles;

        // 🔹 Ensure player's world position matches the tile system
        float localX = (tileFloatX - tileX) * tileManager.tileSize;
        float localY = (tileFloatY - tileY) * tileManager.tileSize;
        localY = -localY;  // Flip Y to match Unity's coordinate system

        //Debug.Log($"X {tileFloatX} Y {tileFloatY}");

        float worldX = (tileX - tileManager.tileX) * tileManager.tileSize - tileManager.tileSize/2 + localX;
        float worldY = (tileManager.tileY - tileY) * tileManager.tileSize + tileManager.tileSize/2 + localY;

        Vector3 worldPos = new Vector3(worldX, 0.2f, worldY);

        return worldPos;
    }

    private void OnApplicationQuit()
    {
        Input.location.Stop();
        Input.compass.enabled = false;
        Input.gyro.enabled = false;
    }
}
