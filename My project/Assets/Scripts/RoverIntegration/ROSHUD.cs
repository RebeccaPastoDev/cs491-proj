using UnityEngine;
using TMPro;

public class ROSHUD : MonoBehaviour
{
    [Header("Telemetry Sources")]
    public RoverROSTelemetryReceiver rosReceiver;
    public UnityBridgeClient bridgeClient;
    public bool useBridgeMode = false;
    public Transform rover;
    public Rigidbody roverRigidbody;

    [Header("Desktop HUD")]
    public TMP_Text positionText;
    public TMP_Text speedText;
    public TMP_Text headingText;

    public TMP_Text batteryText;

    [Header("VR HUD")]
    public TMP_Text vrPositionText;
    public TMP_Text vrSpeedText;
    public TMP_Text vrHeadingText;

    public TMP_Text vrBatteryText;

    [Header("Settings")]
    public bool useROSWhenAvailable = true;

    private bool wasBridgeMode;
    private string previousBattery;
    private string previousVrBattery;

    void LateUpdate()
    {
        if (useBridgeMode)
        {
            if (!wasBridgeMode)
            {
                previousBattery = batteryText != null ? batteryText.text : null;
                previousVrBattery = vrBatteryText != null ? vrBatteryText.text : null;
            }
            wasBridgeMode = true;
            bool active = bridgeClient != null && bridgeClient.TelemetryActive;
            bool odom = active && bridgeClient.hasOdometry;
            string source = active && bridgeClient.isLive ? "ROS live" : "Recorded";
            Vector3 p = odom ? bridgeClient.bridgePosition : Vector3.zero;
            string pos = odom ? $"X: {p.x:F1} Y: {p.y:F1} Z: {p.z:F1} ({source})" : "N/A";
            string spd = odom ? $"{bridgeClient.bridgeSpeed:F2} m/s" : "N/A";
            string hdg = odom ? $"{bridgeClient.bridgeHeading:F1}°" : "N/A";
            string bat = active && bridgeClient.hasBattery ? $"{bridgeClient.batteryVoltage:F1} V" : "N/A";
            UpdateText(positionText, pos);
            UpdateText(speedText, spd);
            UpdateText(headingText, hdg);
            UpdateText(batteryText, bat);
            UpdateText(vrPositionText, pos);
            UpdateText(vrSpeedText, spd);
            UpdateText(vrHeadingText, hdg);
            UpdateText(vrBatteryText, bat);
            return;
        }
        if (wasBridgeMode)
        {
            UpdateText(batteryText, previousBattery);
            UpdateText(vrBatteryText, previousVrBattery);
            wasBridgeMode = false;
        }

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
