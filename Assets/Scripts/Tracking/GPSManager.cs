using UnityEngine;
using System.Collections;

public class GPSManager : MonoBehaviour
{
    public TileLoader tileLoader;
    private bool gpsInitialized = false;
    public PlayerMarker playerMarker;

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

        gpsInitialized = true;
    }

    void Update()
    {
        if (!gpsInitialized || Input.location.status != LocationServiceStatus.Running)
            return;

        float lat = Input.location.lastData.latitude;
        float lon = Input.location.lastData.longitude;

        int tileX, tileY;
        ConvertLatLonToTile(lat, lon, tileLoader.zoom, out tileX, out tileY);

        if (tileX != tileLoader.tileX || tileY != tileLoader.tileY)
        {
            tileLoader.tileX = tileX;
            tileLoader.tileY = tileY;
            tileLoader.StartCoroutine(tileLoader.LoadTile(tileX, tileY, tileLoader.zoom));
        }

        Vector3 playerPosition = new Vector3(tileX, 0, tileY); // Adjust to correct coordinate mapping
        playerMarker.UpdatePosition(playerPosition);
    }


    private void OnApplicationQuit()
    {
        Input.location.Stop();
    }

    void ConvertLatLonToTile(float lat, float lon, int zoom, out int tileX, out int tileY)
    {
        tileX = (int)((lon + 180.0f) / 360.0f * (1 << zoom));
        tileY = (int)((1.0f - Mathf.Log(Mathf.Tan(lat * Mathf.PI / 180.0f) + 1.0f / Mathf.Cos(lat * Mathf.PI / 180.0f)) / Mathf.PI) / 2.0f * (1 << zoom));
    }
}
