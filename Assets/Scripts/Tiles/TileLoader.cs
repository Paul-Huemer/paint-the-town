using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class TileLoader : MonoBehaviour
{
    public int zoom = 14; // Default zoom level
    public int tileX = 1234; // Default X tile index
    public int tileY = 1234; // Default Y tile index
    public Renderer mapRenderer; // The plane or UI element to display the map

    public int offsetX = 0;
    public int offsetY = 0;

    private string tileServerURL = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";

    void Start()
    {
        StartCoroutine(LoadTile(tileX+offsetX, tileY+offsetY, zoom));
    }

    public IEnumerator LoadTile(int x, int y, int z)
    {
        string url = tileServerURL.Replace("{z}", z.ToString())
                                  .Replace("{x}", x.ToString())
                                  .Replace("{y}", y.ToString());

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D tileTexture = DownloadHandlerTexture.GetContent(request);
                mapRenderer.material.mainTexture = tileTexture;
            }
            else
            {
                Debug.LogError("Failed to load tile: " + request.error);
            }
        }
    }

    public void ConvertLatLonToTile(float lat, float lon, int zoom, out int tileX, out int tileY)
        {
            tileX = (int)((lon + 180.0) / 360.0 * (1 << zoom));
            tileY = (int)((1f - Mathf.Log(Mathf.Tan(lat * Mathf.PI / 180f) + 1f / Mathf.Cos(lat * Mathf.PI / 180f)) / Mathf.PI) / 2f * (1 << zoom));
        }
}
