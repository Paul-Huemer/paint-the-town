using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class SavedRoadData
{
    public Vector2Int tile;
    public List<string> roadObjNames = new List<string>();  // Save names or unique IDs
}

[Serializable]
public class SavedRoads
{
    public List<SavedRoadData> roads = new List<SavedRoadData>();  // List of all saved roads
}

public class SavingManager : MonoBehaviour
{
    private string savePath;
    public Dictionary<Vector2Int, List<string>> walkedOnRoads = new Dictionary<Vector2Int, List<string>>();

    public TMP_Text roadSegmentCount;
    public TMP_Text tilesVisitedCount;

    private void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, "walkedOnRoads.json");
        LoadWalkedOnRoads();

        InvokeRepeating(nameof(UpdateRoadSegmentCount), 0f, 5f);
    }

    public void saveWalkedOnRoad(GameObject roadObj, Vector2Int tile)
    {
        if (!walkedOnRoads.ContainsKey(tile))
        {
            walkedOnRoads[tile] = new List<string>();
        }
        walkedOnRoads[tile].Add(roadObj.name);

        SaveWalkedOnRoads();  // Save after every change
    }

    private void SaveWalkedOnRoads()
    {
        SavedRoads savedData = new SavedRoads();

        foreach (var entry in walkedOnRoads)
        {
            SavedRoadData roadData = new SavedRoadData();
            roadData.tile = entry.Key;
            roadData.roadObjNames = entry.Value;

            savedData.roads.Add(roadData);
        }

        string json = JsonUtility.ToJson(savedData, true);
        File.WriteAllText(savePath, json);
        //Debug.Log($"Saved walked roads to: {savePath}");
    }

    private void LoadWalkedOnRoads()
    {
        Debug.Log($"Attempting to load from: {savePath}"); // Add this line

        if (File.Exists(savePath))
        {
            Debug.Log("Save file found, reading..."); // Add this line

            string json = File.ReadAllText(savePath);
            SavedRoads loadedData = JsonUtility.FromJson<SavedRoads>(json);

            walkedOnRoads.Clear();
            foreach (SavedRoadData savedRoad in loadedData.roads)
            {
                walkedOnRoads[savedRoad.tile] = savedRoad.roadObjNames;
            }

            Debug.Log("Loaded walked roads from file.");
        }
        else
        {
            Debug.Log("No save file found.");
        }
    }

    private void UpdateRoadSegmentCount()
    {
        roadSegmentCount.text = $"Segments collected: {GetTotalRoadSegmentCount()}";
        tilesVisitedCount.text = $"Tiles visited: {walkedOnRoads.Count}";
    }

    private int GetTotalRoadSegmentCount()
    {
        int count = 0;
        foreach (var entry in walkedOnRoads)
        {
            count += entry.Value.Count; // Count all road object names in lists
        }
        return count;
    }
}
