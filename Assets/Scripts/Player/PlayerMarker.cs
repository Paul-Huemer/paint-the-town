using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerMarker : MonoBehaviour
{
    public Transform mapTransform; // Reference to the OSM map
    private Vector3 offset = new Vector3(0, 0.1f, 0); // Offset to keep the player above the map

    // Smooth transition variables
    public float smoothSpeed = 1f; // How fast the position moves
    private Vector3 currentPosition; // Current position of the player
    private bool compassInitialized = false;

    void Start() {
        StartCoroutine(StartCompass());
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true; // We don't want physics to affect the player
        CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
        col.height = 0.3f;
        col.radius = 0.3f;
    }

    public void UpdatePosition(Vector3 newPosition)
    {
        float heading = GetDeviceHeading();
        transform.position = Vector3.Lerp(transform.position, newPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(90, heading, 0), smoothSpeed * Time.deltaTime);
    }

    float GetDeviceHeading()
    {
        if (compassInitialized)
        {
            return Input.compass.trueHeading; // Returns heading relative to true north
        }
        else
        {
            return 0f; // Default if compass not available
        }
    }

    IEnumerator StartCompass()
    {
        if (!SystemInfo.supportsGyroscope)
        {
            Debug.LogWarning("Gyroscope not supported.");
        }

        if (!Input.compass.enabled)
        {
            Input.compass.enabled = true;
            Input.gyro.enabled = true;
        }

        yield return new WaitForSeconds(1); // Allow some time for initialization

        if (Input.compass.enabled)
        {
            compassInitialized = true;
            Debug.Log("Compass initialized successfully.");
        }
        else
        {
            Debug.LogWarning("Failed to initialize compass.");
        }
    }
}
