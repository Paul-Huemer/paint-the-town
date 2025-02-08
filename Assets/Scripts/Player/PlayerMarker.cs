using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerMarker : MonoBehaviour
{
    public Transform mapTransform;
    private Vector3 offset = new Vector3(0, 0.1f, 0);

    public float smoothSpeed = 1f;
    private Vector3 currentPosition;
    private bool compassInitialized = false;

    void Start() {
        StartCoroutine(StartCompass());
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
        col.height = 0.3f;
        col.radius = 0.3f;
    }

    public void UpdatePosition(Vector3 newPosition)
    {
        float heading = GetDeviceHeading();
        transform.position = newPosition;
        transform.rotation = Quaternion.Euler(90, heading, 0);
    }

    float GetDeviceHeading()
    {
        if (compassInitialized)
        {
            return Input.compass.trueHeading;
        }
        else
        {
            return 0f;
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

        yield return new WaitForSeconds(1);

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
