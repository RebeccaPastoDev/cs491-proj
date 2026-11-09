
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
    public float acceleration = 10f;
    public float breakingForce = 120f;

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

    // Wheel animation
    private float[] wheelSpins = new float[4];
    private Quaternion[] originalRotations = new Quaternion[4];

    private WheelCollider[] wheelColliders;
    private Transform[] wheelTransforms;

    private void Start()
    {
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

        // Save the original rotation of each wheel
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
        if (frontLeft != null)
        {
            Debug.Log(
                "Front Left Grounded: " + frontLeft.isGrounded +
                " | RPM: " + frontLeft.rpm
            );
        }

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

        // Apply motor torque to all four wheels
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

        // Rear wheels steer in the opposite direction
        backLeft.steerAngle = -currentTurnAngle;
        backRight.steerAngle = -currentTurnAngle;
    }

    private void LateUpdate()
    {
        // Animate all four wheels without changing positions
        for (int i = 0; i < 4; i++)
        {
            if (wheelColliders[i] == null ||
                wheelTransforms[i] == null)
                continue;

            // Convert RPM into degrees per second
            wheelSpins[i] +=
                wheelColliders[i].rpm * 6f * Time.deltaTime;

            // Rotate around the local Y-axis
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
