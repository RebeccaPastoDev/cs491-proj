using UnityEngine;
using TMPro;

public class ROSHUD : MonoBehaviour
{
    [Header("Telemetry Sources")]
    public RoverROSTelemetryReceiver rosReceiver;
    public Transform rover;
    public Rigidbody roverRigidbody;

    [Header("Desktop HUD")]
    public TMP_Text positionText;
    public TMP_Text speedText;
    public TMP_Text headingText;

    [Header("VR HUD")]
    public TMP_Text vrPositionText;
    public TMP_Text vrSpeedText;
    public TMP_Text vrHeadingText;

    [Header("Settings")]
    public bool useROSWhenAvailable = true;

    void LateUpdate()
    {
        if (rover == null)
            return;

        bool useROS =
            useROSWhenAvailable &&
            rosReceiver != null &&
            rosReceiver.telemetryActive;

        Vector3 position;
        float speed;
        float heading;

        if (useROS)
        {
            position = rosReceiver.rosPosition;
            speed = rosReceiver.rosSpeed;
            heading = rosReceiver.rosHeading;
        }
        else
        {
            position = rover.position;

            speed = roverRigidbody != null
                ? roverRigidbody.linearVelocity.magnitude
                : 0f;

            heading = rover.eulerAngles.y;
        }

        string positionValue =
            $"X: {position.x:F1}  " +
            $"Y: {position.y:F1}  " +
            $"Z: {position.z:F1}";

        string speedValue = $"{speed:F2} m/s";
        string headingValue = $"{heading:F1}°";

        UpdateText(positionText, positionValue);
        UpdateText(speedText, speedValue);
        UpdateText(headingText, headingValue);

        UpdateText(vrPositionText, positionValue);
        UpdateText(vrSpeedText, speedValue);
        UpdateText(vrHeadingText, headingValue);
    }

    void UpdateText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}
