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

    // Degrees per second
    public float steeringSpeed = 30f;

    // How quickly wheels return to center
    public float steeringReturnSpeed = 40f;

    [Header("Current Values")]
    public float currentAcceleration;
    public float currentBreakForce;
    public float currentTurnAngle;

    private float verticalInput;
    private float horizontalInput;
    private bool brakeInput;
    // Prevents driving while the rover is resetting
    private bool isResetting = false;

    private void Update()
    {
        if (isResetting)
            return;

        verticalInput = Input.GetAxis("Vertical");

        // Raw input prevents additional input smoothing
        horizontalInput = Input.GetAxisRaw("Horizontal");

        brakeInput = Input.GetKey(KeyCode.Space);
    }

    private void FixedUpdate()
    {
        if (isResetting)
            return;

        // ACCELERATION
        currentAcceleration = brakeInput
            ? 0f
            : acceleration * verticalInput;

        currentBreakForce = brakeInput
            ? breakingForce
            : 0f;

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

        // Front wheels turn together
        frontLeft.steerAngle = currentTurnAngle;
        frontRight.steerAngle = currentTurnAngle;

        // Rear wheels turn in opposite direction
        backLeft.steerAngle = -currentTurnAngle;
        backRight.steerAngle = -currentTurnAngle;

    }

    private void LateUpdate()
    {
        UpdateWheel(frontLeft, frontLeftTransform);
        UpdateWheel(frontRight, frontRightTransform);
        UpdateWheel(backLeft, backLeftTransform);
        UpdateWheel(backRight, backRightTransform);
    }

    private void UpdateWheel(
        WheelCollider col,
        Transform wheel)
    {
        if (col == null || wheel == null)
            return;

        col.GetWorldPose(
            out Vector3 position,
            out Quaternion rotation
        );

        wheel.SetPositionAndRotation(position, rotation);
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
