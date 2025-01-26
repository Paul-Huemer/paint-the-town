using UnityEngine;
using System.Collections.Generic;

public class TileManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public Dictionary<Vector2Int, GameObject> activeTiles = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int currentTile;

    public OSMDataFetcher osmDataFetcher;

    public int tileX = -1;
    public int tileY = -1;
    public int zoom = 14;

    public int tileSize = 10;
    public Transform player; // Assign the player GameObject

    private void Start()
    {
        currentTile = GetTileCoordinates(player.position);
        LoadTilesAround(currentTile);
    }

    private void Update()
    {
        Vector2Int newTile = GetTileCoordinates(player.position);

        // ✅ Trigger only when the player LEAVES the current tile
        if (newTile != currentTile)
        {
            Debug.Log($"Player left tile {currentTile} -> Now in {newTile}");
            currentTile = newTile;
            LoadTilesAround(currentTile);
        }
    }

    private Vector2Int GetTileCoordinates(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt((position.x + tileSize / 2) / tileSize),
            Mathf.FloorToInt((position.z + tileSize / 2) / tileSize)
        );
    }

    public void LoadTilesAround(Vector2Int center)
    {
        // Unload old tiles
        foreach (var key in new List<Vector2Int>(activeTiles.Keys))
        {
            if (Mathf.Abs(key.x - center.x) > 1 || Mathf.Abs(key.y - center.y) > 1)
            {
                Destroy(activeTiles[key]);
                activeTiles.Remove(key);
            }
        }

        // Load new tiles (3x3 grid)
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int tileCoords = new Vector2Int(x, y);
                if (!activeTiles.ContainsKey(tileCoords))
                {
                    GameObject newTile = Instantiate(tilePrefab, new Vector3(tileCoords.x * tileSize, 0, tileCoords.y * tileSize), Quaternion.identity);

                    Vector2Int tileConvertedCoords = new Vector2Int(center.x + x, center.y - y);
                    activeTiles[tileCoords] = newTile;

                    TileLoader loader = newTile.GetComponent<TileLoader>();
                    if (loader != null && center.x >= 0 && center.y >= 0)
                    {
                        // ✅ Corrected Tile Coordinate Calculation
                        loader.LoadTile(center.x + x, center.y - y, zoom);
                    }
                }
            }
        }

        if (osmDataFetcher != null)
        {
            osmDataFetcher.LoadRoadsForNewTile(new Vector2Int(center.x, center.y));
        }
    }

    public void ConvertLatLonToTile(float lat, float lon, int zoom, out int tileX, out int tileY)
    {
        tileX = (int)((lon + 180.0) / 360.0 * (1 << zoom));
        tileY = (int)((1f - Mathf.Log(Mathf.Tan(lat * Mathf.PI / 180f) + 1f / Mathf.Cos(lat * Mathf.PI / 180f)) / Mathf.PI) / 2f * (1 << zoom));
    }
}