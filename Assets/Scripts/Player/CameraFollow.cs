using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform playerMarker; // Assign PlayerMarker in Inspector
    public Vector3 offset = new Vector3(0, 5f, -5f); // Adjust as needed
    public bool lockRotation = true; // Toggle this if you want the camera to rotate

    void LateUpdate()
    {
        if (playerMarker == null) return;

        // Keep camera centered on PlayerMarker
        transform.position = playerMarker.position + offset;

        // Prevent rotation (keep it fixed)
        if (lockRotation)
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Adjust as needed
        }
    }
}
