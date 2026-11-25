using UnityEngine;

public class OldRoverScript : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontRight;
    public WheelCollider frontLeft;
    public WheelCollider backRight;
    public WheelCollider backLeft;

    [Header("Wheel Meshes")]
    public Transform frontLeftTransform;
    public Transform backLeftTransform;
    public Transform frontRightTransform;
    public Transform backRightTransform;

    [Header("Movement Settings")]
    public float acceleration = 15f;
    public float breakingForce = 1f;

    [Header("Steering Settings")]
    public float maxTurnAngle = 8f;
    public float steeringSpeed = 10f;
    public float steeringReturnSpeed = 15f;

    [Header("Current Values")]
    public float currentAcceleration = 0f;
    public float currentBreakForce = 0f;
    public float currentTurnAngle = 0f;

    private void FixedUpdate()
    {
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");

        currentAcceleration = acceleration * verticalInput;
        currentBreakForce = Input.GetKey(KeyCode.Space)
            ? breakingForce : 0f;

        float targetAngle = maxTurnAngle * horizontalInput;

        float speed = Mathf.Abs(horizontalInput) > 0.01f
            ? steeringSpeed : steeringReturnSpeed;

        currentTurnAngle = Mathf.MoveTowards(
            currentTurnAngle,
            targetAngle,
            speed * Time.fixedDeltaTime
        );

        frontLeft.steerAngle = currentTurnAngle;
        frontRight.steerAngle = currentTurnAngle;

        frontLeft.motorTorque = currentAcceleration;
        frontRight.motorTorque = currentAcceleration;
        backLeft.motorTorque = currentAcceleration;
        backRight.motorTorque = currentAcceleration;

        frontLeft.brakeTorque = currentBreakForce;
        frontRight.brakeTorque = currentBreakForce;
        backLeft.brakeTorque = currentBreakForce;
        backRight.brakeTorque = currentBreakForce;

        UpdateWheel(frontLeft, frontLeftTransform);
        UpdateWheel(frontRight, frontRightTransform);
        UpdateWheel(backLeft, backLeftTransform);
        UpdateWheel(backRight, backRightTransform);
    }

    private void UpdateWheel(
        WheelCollider wheelCollider,
        Transform wheelTransform)
    {
        if (wheelCollider == null || wheelTransform == null)
            return;

        wheelCollider.GetWorldPose(
            out Vector3 position,
            out Quaternion rotation
        );

        wheelTransform.position = position;
        wheelTransform.rotation = rotation;
    }

    public void ResetRoverControls()
    {
        currentAcceleration = 0f;
        currentBreakForce = 0f;
        currentTurnAngle = 0f;

        foreach (WheelCollider wheel in new[]
        {
            frontLeft, frontRight, backLeft, backRight
        })
        {
            if (wheel == null) continue;

            wheel.motorTorque = 0f;
            wheel.brakeTorque = 0f;
            wheel.steerAngle = 0f;
        }
    }
}
