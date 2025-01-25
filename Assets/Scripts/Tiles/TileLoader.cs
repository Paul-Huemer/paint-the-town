using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class TileLoader : MonoBehaviour
{
    public Renderer mapRenderer; // The plane or UI element to display the map
    private string tileServerURL = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";

    public void LoadTile(int x, int y, int zoom)
    {
        StartCoroutine(LoadTileTexture(x, y, zoom));
    }

    private IEnumerator LoadTileTexture(int x, int y, int zoom)
    {
        string url = tileServerURL.Replace("{z}", zoom.ToString())
                                  .Replace("{x}", x.ToString())
                                  .Replace("{y}", y.ToString());


        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D tileTexture = DownloadHandlerTexture.GetContent(request);
                mapRenderer.material.SetTexture("_MainTex", tileTexture);
                transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                Debug.LogError("Failed to load tile: " + request.error);
            }
        }
    }

    private Texture2D FlipTexture(Texture2D original)
    {
        Texture2D flipped = new Texture2D(original.width, original.height);
        for (int i = 0; i < original.width; i++)
        {
            for (int j = 0; j < original.height; j++)
            {
                flipped.SetPixel(i, j, original.GetPixel(original.width - i - 1, original.height - j - 1));
            }
        }
        flipped.Apply();
        return flipped;
    }
}
