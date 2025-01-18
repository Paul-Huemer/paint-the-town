using UnityEngine;

public class PlayerMarker : MonoBehaviour
{
    public Transform mapTransform; // Reference to the OSM map
    private Vector3 offset = new Vector3(0, 0.1f, 0); // Offset to keep the player above the map

    public void UpdatePosition(Vector3 newPosition)
    {
        transform.position = newPosition + offset;
    }
}
