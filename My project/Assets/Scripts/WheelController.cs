using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class RoverControllerNew : MonoBehaviour
{
    [SerializeField] WheelCollider frontRight;
    [SerializeField] WheelCollider frontLeft;
    [SerializeField] WheelCollider backRight;
    [SerializeField] WheelCollider backLeft;

    [SerializeField] Transform frontLeftTransform;
    [SerializeField] Transform backLeftTransform;
    [SerializeField] Transform frontRightTransform;
    [SerializeField] Transform backRightTransform;


    public float acceleration = 250f;
    public float breakingForce = 120f;
    public float maxTurnAngle = 15f;


    public float currentAcceleration = 0f;
    public float currentBreakForce = 0f;
    public float currentTurnAngle = 0f;


    private void FixedUpdate()
    {

        currentAcceleration = acceleration * Input.GetAxis("Vertical");
        if (Input.GetKey(KeyCode.Space))
            currentBreakForce = breakingForce;
        else
            currentBreakForce = 0f;
        frontRight.motorTorque = currentAcceleration;
        frontLeft.motorTorque = currentAcceleration;
        backRight.motorTorque = currentAcceleration;
        backLeft.motorTorque = currentAcceleration;

        frontRight.brakeTorque = currentBreakForce;
        frontLeft.brakeTorque = currentBreakForce;
        backLeft.brakeTorque = currentBreakForce;
        backRight.brakeTorque = currentBreakForce;


        currentTurnAngle = maxTurnAngle * Input.GetAxis("Horizontal");
        frontLeft.steerAngle = currentTurnAngle;
        frontRight.steerAngle = currentTurnAngle;
        backLeft.steerAngle = -currentTurnAngle;
        backRight.steerAngle = -currentTurnAngle;

        UpdateWheel(frontLeft, frontLeftTransform);
        UpdateWheel(frontRight, frontRightTransform);
        UpdateWheel(backLeft, backLeftTransform);
        UpdateWheel(backRight, backRightTransform);

    }

    void UpdateWheel (WheelCollider col, Transform trans)
    {
        Vector3 position;
        Quaternion rotation;
        col.GetWorldPose(out position, out rotation);
        

        trans.position = position;
        trans.rotation = rotation;

    }

}
