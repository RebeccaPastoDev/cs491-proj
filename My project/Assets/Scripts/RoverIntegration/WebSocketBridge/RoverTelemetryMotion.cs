using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RoverTelemetryMotion : MonoBehaviour
{
    public enum MovementMode { ManualSimulation, RecordedPlayback }
    public MovementMode movementMode = MovementMode.RecordedPlayback;
    public UnityBridgeClient bridgeClient;
    public MecanumRoverController manualController;
    [Min(0.0001f)] public float metersPerUnityUnit = 0.03048f;
    [Min(0.01f)] public float positionGain = 5f;
    [Min(0.01f)] public float maxTrackingSpeed = 30f;
    [Min(0.01f)] public float maxRecordedStepMeters = 0.25f;
    [Min(1f)] public float maxTurnSpeed = 180f;
    public string playbackStatus;
    public bool animateRecordedWheels = true;
    private bool wheelSampleReady;
    private Vector3 previousWheelPosition;
    private Quaternion previousWheelRotation;

    private Rigidbody rb;
    private bool anchored;
    private bool ownsMovement;
    private bool previousManualEnabled;
    private int generation;
    private Vector3 lastRecorded, target;
    private float lastHeading, targetYaw, alignment;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (manualController == null)
            manualController = GetComponent<MecanumRoverController>();
    }

    void StopHorizontal()
    {
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.up * rb.linearVelocity.y;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void ReleaseMovement()
    {
        if (!ownsMovement) return;
        StopHorizontal();
        if (manualController != null) manualController.enabled = previousManualEnabled;
        ownsMovement = false;
        anchored = false;
        wheelSampleReady = false;
    }

    void OnDisable() { if (rb != null) ReleaseMovement(); }

    void FixedUpdate()
    {
        if (movementMode == MovementMode.ManualSimulation)
        {
            ReleaseMovement();
            playbackStatus = "Manual simulation";
            return;
        }
        if (!ownsMovement)
        {
            previousManualEnabled = manualController != null && manualController.enabled;
            if (manualController != null) manualController.enabled = false;
            ownsMovement = true;
        }
        if (rb.isKinematic)
        {
            wheelSampleReady = false;
            anchored = false;
            playbackStatus = "Waiting for terrain / physics";
            return;
        }
        if (bridgeClient == null || !bridgeClient.TelemetryActive ||
            !bridgeClient.hasOdometry || bridgeClient.isLive ||
            bridgeClient.physicalExecutionEnabled || bridgeClient.telemetrySource != "recorded")
        {
            wheelSampleReady = false;
            anchored = false;
            StopHorizontal();
            playbackStatus = "Waiting for recorded odometry";
            return;
        }
        Vector3 p = bridgeClient.bridgePosition;
        float heading = bridgeClient.bridgeHeading;
        if (!Finite(p.x) || !Finite(p.y) || !Finite(heading))
        {
            wheelSampleReady = false;
            anchored = false;
            StopHorizontal();
            playbackStatus = "Invalid odometry";
            return;
        }
        if (!anchored || generation != bridgeClient.connectionGeneration)
        {
            wheelSampleReady = false;
            anchored = true;
            generation = bridgeClient.connectionGeneration;
            lastRecorded = p;
            lastHeading = heading;
            target = rb.position;
            targetYaw = rb.rotation.eulerAngles.y;
            alignment = targetYaw + heading;
        }
        Vector3 delta = p - lastRecorded;
        lastRecorded = p;
        float yawDelta = Mathf.DeltaAngle(lastHeading, heading);
        lastHeading = heading;
        Vector3 horizontal = new Vector3(-delta.y, 0f, delta.x);
        if (horizontal.magnitude > maxRecordedStepMeters || Mathf.Abs(yawDelta) > 45f)
        {
            wheelSampleReady = false;
            anchored = false;
            StopHorizontal();
            playbackStatus = "Odometry jump ignored";
            return;
        }
        target += Quaternion.Euler(0f, alignment, 0f) * horizontal / Mathf.Max(metersPerUnityUnit, 0.0001f);
        targetYaw -= yawDelta;
        Vector3 error = target - rb.position;
        error.y = 0f;
        Vector3 velocity = Vector3.ClampMagnitude(error * positionGain, maxTrackingSpeed);
        rb.linearVelocity = velocity + Vector3.up * rb.linearVelocity.y;
        rb.angularVelocity = Vector3.zero;
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, Quaternion.Euler(0f, targetYaw, 0f), maxTurnSpeed * Time.fixedDeltaTime));
        AnimateRecordedWheels();
        playbackStatus = "Tracking recorded odometry";
    }

    void AnimateRecordedWheels()
    {
        if (!animateRecordedWheels || manualController == null || !manualController.animateWheels)
        {
            wheelSampleReady = false;
            return;
        }
        Vector3 position = rb.position;
        Quaternion rotation = rb.rotation;
        if (wheelSampleReady)
        {
            Vector3 delta = position - previousWheelPosition;
            delta.y = 0f;
            Vector3 local = Quaternion.Inverse(previousWheelRotation) * delta;
            float yaw = Mathf.DeltaAngle(previousWheelRotation.eulerAngles.y, rotation.eulerAngles.y);
            float radius = Mathf.Max(manualController.wheelRadius, 0.001f);
            float factor = Mathf.Rad2Deg / radius * manualController.wheelAnimationMultiplier;
            // Visual approximation: forward/backward axle spin only.
            // Use the same mecanum mixing signs as the manual controller.
            float turnDistance = yaw * Mathf.Deg2Rad * radius;
            Spin(manualController.frontLeft, (local.z + local.x + turnDistance) * factor);
            Spin(manualController.frontRight, (local.z - local.x - turnDistance) * factor);
            Spin(manualController.rearLeft, (local.z - local.x + turnDistance) * factor);
            Spin(manualController.rearRight, (local.z + local.x - turnDistance) * factor);
        }
        previousWheelPosition = position;
        previousWheelRotation = rotation;
        wheelSampleReady = true;
    }

    void Spin(Transform wheel, float angle)
    {
        if (wheel != null) wheel.Rotate(transform.right, angle, Space.World);
    }

    static bool Finite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }
}
