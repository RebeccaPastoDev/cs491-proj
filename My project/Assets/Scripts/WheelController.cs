
using UnityEngine;

public class RoverControllerNew : MonoBehaviour
{
    [Header("Wheel Colliders")]
    [SerializeField] WheelCollider frontRight;
    [SerializeField] WheelCollider frontLeft;
    [SerializeField] WheelCollider backRight;
    [SerializeField] WheelCollider backLeft;

    [Header("Wheel Meshes")]
    [SerializeField] Transform frontLeftTransform;
    [SerializeField] Transform backLeftTransform;
    [SerializeField] Transform frontRightTransform;
    [SerializeField] Transform backRightTransform;

    [Header("Movement Settings")]
    public float acceleration = 1000f;
    public float breakingForce = 1f;

    [Header("Steering Settings")]
    public float maxTurnAngle = 15f;
    public float steeringSpeed = 30f;
    public float steeringReturnSpeed = 40f;

    [Header("Current Values")]
    public float currentAcceleration;
    public float currentBreakForce;
    public float currentTurnAngle;

    private float verticalInput;
    private float horizontalInput;
    private bool brakeInput;
    private bool isResetting = false;

    private Rigidbody roverRb;

    // Wheel animation
    private float[] wheelSpins = new float[4];
    private Quaternion[] originalRotations = new Quaternion[4];

    private WheelCollider[] wheelColliders;
    private Transform[] wheelTransforms;

    // Diagnostic timer
    private float debugTimer = 0f;

    private void Start()
    {
        roverRb = GetComponent<Rigidbody>();

        wheelColliders = new WheelCollider[]
        {
            frontLeft,
            frontRight,
            backLeft,
            backRight
        };

        wheelTransforms = new Transform[]
        {
            frontLeftTransform,
            frontRightTransform,
            backLeftTransform,
            backRightTransform
        };

        // Preserve each wheel's original rotation
        for (int i = 0; i < 4; i++)
        {
            if (wheelTransforms[i] != null)
            {
                originalRotations[i] =
                    wheelTransforms[i].localRotation;
            }
        }
    }

    private void Update()
    {
        if (isResetting)
            return;

        verticalInput = Input.GetAxis("Vertical");
        horizontalInput = Input.GetAxisRaw("Horizontal");
        brakeInput = Input.GetKey(KeyCode.Space);
    }

    private void FixedUpdate()
    {
        if (isResetting)
            return;

        if (frontLeft == null || frontRight == null ||
            backLeft == null || backRight == null)
            return;

        // ACCELERATION
        currentAcceleration = brakeInput
            ? 0f
            : acceleration * verticalInput;

        currentBreakForce = brakeInput
            ? breakingForce
            : 0f;

        // Apply motor torque
        frontRight.motorTorque = currentAcceleration;
        frontLeft.motorTorque = currentAcceleration;
        backRight.motorTorque = currentAcceleration;
        backLeft.motorTorque = currentAcceleration;

        // BRAKING
        frontRight.brakeTorque = currentBreakForce;
        frontLeft.brakeTorque = currentBreakForce;
        backRight.brakeTorque = currentBreakForce;
        backLeft.brakeTorque = currentBreakForce;

        // GRADUAL STEERING
        float targetAngle = maxTurnAngle * horizontalInput;

        float speed = Mathf.Abs(horizontalInput) > 0.01f
            ? steeringSpeed
            : steeringReturnSpeed;

        currentTurnAngle = Mathf.MoveTowards(
            currentTurnAngle,
            targetAngle,
            speed * Time.fixedDeltaTime
        );

        // Front wheels steer together
        frontLeft.steerAngle = currentTurnAngle;
        frontRight.steerAngle = currentTurnAngle;

        // Rear wheels steer in opposite direction
        backLeft.steerAngle = -currentTurnAngle;
        backRight.steerAngle = -currentTurnAngle;

        // Run diagnostics once per second
        debugTimer += Time.fixedDeltaTime;

        if (debugTimer >= 1f)
        {
            debugTimer = 0f;
            LogDiagnostics();
        }
    }

    private void LogDiagnostics()
    {
        Debug.Log(
            "Input: " + verticalInput +
            " | Motor Torque: " + currentAcceleration +
            " | Brake: " + brakeInput
        );

        Debug.Log(
            "Front Left Grounded: " + frontLeft.isGrounded +
            " | RPM: " + frontLeft.rpm.ToString("F3")
        );

        // Check actual wheel traction
        WheelHit hit;

        if (frontLeft.GetGroundHit(out hit))
        {
            Debug.Log(
                "Forward Slip: " +
                hit.forwardSlip.ToString("F3") +
                " | Sideways Slip: " +
                hit.sidewaysSlip.ToString("F3")
            );
        }
        else
        {
            Debug.Log("Front Left: No ground contact");
        }

        // Check Rigidbody movement
        if (roverRb != null)
        {
            Vector3 velocity = roverRb.linearVelocity;

            Debug.Log(
                "Rover Speed: " +
                velocity.magnitude.ToString("F3") +
                " m/s"
            );

            Debug.Log(
                "Velocity X: " + velocity.x.ToString("F3") +
                " | Y: " + velocity.y.ToString("F3") +
                " | Z: " + velocity.z.ToString("F3")
            );

            // Track forward movement along rover's local Z-axis
            float forwardSpeed =
                Vector3.Dot(velocity, transform.forward);

            Debug.Log(
                "Forward Speed: " +
                forwardSpeed.ToString("F3") +
                " m/s"
            );
        }
    }

    private void LateUpdate()
    {
        // Animate wheels around their local Y-axis
        for (int i = 0; i < 4; i++)
        {
            if (wheelColliders[i] == null ||
                wheelTransforms[i] == null)
                continue;

            // Convert RPM into degrees per second
            wheelSpins[i] +=
                wheelColliders[i].rpm * 6f * Time.deltaTime;

            wheelTransforms[i].localRotation =
                originalRotations[i] *
                Quaternion.Euler(0f, wheelSpins[i], 0f);
        }
    }

    // Called by the HUD Reset button
    public void ResetRoverControls()
    {
        isResetting = true;

        verticalInput = 0f;
        horizontalInput = 0f;
        brakeInput = false;

        currentAcceleration = 0f;
        currentBreakForce = 0f;
        currentTurnAngle = 0f;

        WheelCollider[] wheels =
        {
            frontRight,
            frontLeft,
            backRight,
            backLeft
        };

        foreach (WheelCollider wheel in wheels)
        {
            if (wheel == null)
                continue;

            wheel.motorTorque = 0f;
            wheel.brakeTorque = 0f;
            wheel.steerAngle = 0f;
        }

        isResetting = false;
    }
}
