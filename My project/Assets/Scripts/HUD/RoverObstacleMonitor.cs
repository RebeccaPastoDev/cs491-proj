using UnityEngine;
using TMPro;

[DefaultExecutionOrder(100)]
public class RoverObstacleMonitor : MonoBehaviour
{
    [Header("Rover References")]
    public Transform rover;
    public Transform lidarSensor;

    [Header("Simulated LiDAR")]
    public float detectionRange = 50f;
    public float sensorRadius = 3f;
    public float scanHeight = 15f;
    public int scanLevels = 5;

    [Header("Detection Settings")]
    public LayerMask obstacleLayers = ~0;
    public float cautionDistance = 10f;
    public float warningDistance = 5f;

    [Header("Bridge LiDAR")]
    public ROSHUD telemetryHUD;
    public float bridgeCautionDistance = 0.5f;
    public float bridgeWarningDistance = 0.25f;
    public bool BridgeMode => telemetryHUD != null && telemetryHUD.useBridgeMode;
    public bool BridgeScanAvailable => BridgeMode && telemetryHUD.bridgeClient != null &&
        telemetryHUD.bridgeClient.TelemetryActive && telemetryHUD.bridgeClient.hasLidar;
    private bool? previousBridgeAvailability;
    public float ActiveCautionDistance => BridgeMode ? bridgeCautionDistance : cautionDistance;
    public float ActiveWarningDistance => BridgeMode ? bridgeWarningDistance : warningDistance;

    [Header("Desktop HUD")]
    public TMP_Text obstacleValue;

    [Header("VR HUD")]
    public TMP_Text vrObstacleValue;

    // Values accessible by RoverWarningManager.
    public bool HasObstacle { get; private set; }
    public float ClosestObstacleDistance { get; private set; }
    public string ClosestObstacleDirection { get; private set; }

    private readonly string[] directionNames =
    {
        "Ahead",
        "Behind",
        "Left",
        "Right"
    };

    void Update()
    {
        if (!BridgeMode) previousBridgeAvailability = null;
        if (BridgeMode)
        {
            UpdateBridgeObstacle();
            return;
        }
        if (rover == null || lidarSensor == null)
        {
            HasObstacle = false;
            ClosestObstacleDistance = Mathf.Infinity;
            ClosestObstacleDirection = "";

            UpdateObstacleDisplay(
                "Sensor unavailable",
                Color.gray
            );

            return;
        }

        Vector3[] directions =
        {
            rover.forward,
            -rover.forward,
            -rover.right,
            rover.right
        };

        float closestDistance = Mathf.Infinity;
        string closestDirection = "";

        int levels = Mathf.Max(1, scanLevels);

        // Scan at multiple heights below the LiDAR sensor.
        for (int level = 0; level < levels; level++)
        {
            float offset = levels == 1
                ? 0f
                : scanHeight * level / (levels - 1);

            Vector3 origin =
                lidarSensor.position - Vector3.up * offset;

            // Scan in four directions.
            for (int i = 0; i < directions.Length; i++)
            {
                RaycastHit[] hits = Physics.SphereCastAll(
                    origin,
                    sensorRadius,
                    directions[i],
                    detectionRange,
                    obstacleLayers,
                    QueryTriggerInteraction.Ignore
                );

                foreach (RaycastHit hit in hits)
                {
                    // Ignore the rover's own colliders.
                    if (hit.collider.transform == rover ||
                        hit.collider.transform.IsChildOf(rover))
                    {
                        continue;
                    }

                    if (hit.distance < closestDistance)
                    {
                        closestDistance = hit.distance;
                        closestDirection = directionNames[i];
                    }
                }
            }
        }

        // No obstacle detected.
        if (closestDistance == Mathf.Infinity)
        {
            HasObstacle = false;
            ClosestObstacleDistance = Mathf.Infinity;
            ClosestObstacleDirection = "";

            UpdateObstacleDisplay(
                "Clear",
                new Color32(119, 201, 154, 255)
            );

            return;
        }

        // Store values for RoverWarningManager.
        HasObstacle = true;
        ClosestObstacleDistance = closestDistance;
        ClosestObstacleDirection = closestDirection;

        Color displayColor;

        if (closestDistance <= warningDistance)
        {
            // Red: obstacle very close.
            displayColor = new Color32(
                255, 100, 100, 255
            );
        }
        else if (closestDistance <= cautionDistance)
        {
            // Yellow: obstacle nearby.
            displayColor = new Color32(
                255, 200, 80, 255
            );
        }
        else
        {
            // Green: obstacle outside caution distance.
            displayColor = new Color32(
                119, 201, 154, 255
            );
        }

        UpdateObstacleDisplay(
            $"{closestDirection}: {closestDistance:F1} m",
            displayColor
        );
    }

    void UpdateBridgeObstacle()
    {
        var bridge = telemetryHUD.bridgeClient;
        bool available = BridgeScanAvailable;
        if (previousBridgeAvailability != available)
        {
            Debug.Log("[Bridge LiDAR] Scan available: " + available, this);
            previousBridgeAvailability = available;
        }
        HasObstacle = BridgeScanAvailable;
        ClosestObstacleDistance = BridgeScanAvailable ? bridge.lidarClosestDistance : Mathf.Infinity;
        ClosestObstacleDirection = "";
        if (!BridgeScanAvailable)
        {
            UpdateObstacleDisplay("LiDAR unavailable", Color.gray);
            return;
        }
        float d = ClosestObstacleDistance;
        Color color = d <= bridgeWarningDistance ? new Color32(255, 100, 100, 255) :
            d <= bridgeCautionDistance ? new Color32(255, 200, 80, 255) :
            new Color32(119, 201, 154, 255);
        string source = bridge.isLive ? "Live" : "Recorded";
        UpdateObstacleDisplay($"{d:F2} m ({source})", color);
    }

    void UpdateObstacleDisplay(string value, Color color)
    {
        // Desktop HUD.
        if (obstacleValue != null)
        {
            obstacleValue.text = value;
            obstacleValue.color = color;
        }

        // VR HUD.
        if (vrObstacleValue != null)
        {
            vrObstacleValue.text = value;
            vrObstacleValue.color = color;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (rover == null || lidarSensor == null)
            return;

        Gizmos.color = Color.cyan;

        Vector3[] directions =
        {
            rover.forward,
            -rover.forward,
            -rover.right,
            rover.right
        };

        int levels = Mathf.Max(1, scanLevels);

        for (int level = 0; level < levels; level++)
        {
            float offset = levels == 1
                ? 0f
                : scanHeight * level / (levels - 1);

            Vector3 origin =
                lidarSensor.position - Vector3.up * offset;

            Gizmos.DrawWireSphere(
                origin,
                sensorRadius
            );

            foreach (Vector3 direction in directions)
            {
                Gizmos.DrawRay(
                    origin,
                    direction * detectionRange
                );
            }
        }
    }
}
