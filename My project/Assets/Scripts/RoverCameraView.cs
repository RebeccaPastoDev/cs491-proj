
using UnityEngine;

public class RoverCameraView : MonoBehaviour
{
    [Header("Camera Rotation")]
    public float turnSpeed = 80f;
    public float lookSpeed = 60f;

    [Header("Vertical Look Limits")]
    public float minPitch = -60f;
    public float maxPitch = 60f;

    private float yaw;
    private float pitch;

    void Start()
    {
        // Preserve the camera's initial local rotation.
        Vector3 angles = transform.localEulerAngles;

        yaw = NormalizeAngle(angles.y);
        pitch = NormalizeAngle(angles.x);
    }

    void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;

        // Turn left/right like a human head.
        if (Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;

        if (Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;

        // Look up/down.
        if (Input.GetKey(KeyCode.UpArrow))
            vertical = -1f;

        if (Input.GetKey(KeyCode.DownArrow))
            vertical = 1f;

        yaw += horizontal * turnSpeed * Time.deltaTime;

        pitch += vertical * lookSpeed * Time.deltaTime;

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );

        // Rotate camera locally, without moving it.
        transform.localRotation =
            Quaternion.Euler(pitch, yaw, 0f);
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}
