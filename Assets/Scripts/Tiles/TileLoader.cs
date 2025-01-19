using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class TileLoader : MonoBehaviour
{
    public int zoom = 14; // Default zoom level
    public int tileX = 1234; // Default X tile index
    public int tileY = 1234; // Default Y tile index
    public Renderer mapRenderer; // The plane or UI element to display the map

    private string tileServerURL = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";

    void Start()
    {
        StartCoroutine(LoadTile(tileX, tileY, zoom));
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
}
