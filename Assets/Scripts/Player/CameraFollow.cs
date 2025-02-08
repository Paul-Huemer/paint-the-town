using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform playerMarker;
    public Vector3 offset = new Vector3(0, 5f, -5f);
    public bool lockRotation = true;

    void LateUpdate()
    {
        if (playerMarker == null) return;
        transform.position = playerMarker.position + offset;
        if (lockRotation)
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
