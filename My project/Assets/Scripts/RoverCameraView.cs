using UnityEngine;

public class RoverCameraView : MonoBehaviour
{
    [Header("Camera Rotation")]
    public float rotationSpeed = 60f;

    void Update()
    {
        float horizontal = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;

        if (Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;

        transform.Rotate(
            0f,
            horizontal * rotationSpeed * Time.deltaTime,
            0f,
            Space.Self
        );
    }
}
