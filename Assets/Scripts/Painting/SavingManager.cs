using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavingManager : MonoBehaviour
{

    public Dictionary<Vector2Int, List<GameObject>> walkedOnRoads = new Dictionary<Vector2Int, List<GameObject>>();

    public void saveWalkedOnRoad(GameObject roadObj, Vector2Int tile) {
            if (!walkedOnRoads.ContainsKey(tile)) {
                List<GameObject> roadObjList = new List<GameObject>();
                walkedOnRoads[tile] = roadObjList;
            }
            walkedOnRoads[tile].Add(roadObj);
        }
}
