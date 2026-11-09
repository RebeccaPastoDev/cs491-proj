
using UnityEngine;
using System.Collections.Generic;

public class RoverPhysicsDiagnostic : MonoBehaviour
{
    [Header("References")]
    public Rigidbody roverRb;
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider backLeft;
    public WheelCollider backRight;
    public Collider chassisCollider;

    [Header("Diagnostic Settings")]
    public float logInterval = 1f;
    public bool checkTerrain = true;

    private float timer;
    private Vector3 previousPosition;
    private WheelCollider[] wheels;
    private string[] wheelNames;

    void Start()
    {
        if (roverRb == null)
            roverRb = GetComponent<Rigidbody>();

        wheels = new WheelCollider[]
        {
            frontLeft, frontRight, backLeft, backRight
        };

        wheelNames = new string[]
        {
            "FL", "FR", "BL", "BR"
        };

        previousPosition = transform.position;

        if (roverRb == null)
        {
            Debug.LogError("DIAGNOSTIC: Rigidbody missing!");
            enabled = false;
            return;
        }

        Debug.Log("========== ROVER DIAGNOSTIC START ==========");

        Debug.Log(
            "Mass: " + roverRb.mass +
            " | Gravity: " + Physics.gravity +
            " | Scale: " + transform.lossyScale
        );

        foreach (WheelCollider wheel in wheels)
        {
            if (wheel == null)
                continue;

            JointSpring spring = wheel.suspensionSpring;

            Debug.Log(
                wheel.name +
                " | Radius: " + wheel.radius +
                " | Suspension: " + wheel.suspensionDistance +
                " | Spring: " + spring.spring +
                " | Damper: " + spring.damper +
                " | Position: " + wheel.transform.position
            );
        }
    }

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;

        if (timer < logInterval)
            return;

        timer = 0f;

        RunDiagnostics();
    }

    void RunDiagnostics()
    {
        Vector3 velocity = roverRb.linearVelocity;
        Vector3 angularVelocity = roverRb.angularVelocity;

        Vector3 displacement =
            transform.position - previousPosition;

        previousPosition = transform.position;

        Debug.Log(
            "========== ROVER PHYSICS REPORT ==========\n" +
            "Position: " + transform.position + "\n" +
            "Displacement: " + displacement + "\n" +
            "Velocity: " + velocity + "\n" +
            "Speed: " + velocity.magnitude.ToString("F2") + " m/s\n" +
            "Forward Speed: " +
            Vector3.Dot(velocity, transform.forward).ToString("F2") +
            " m/s\n" +
            "Angular Velocity: " + angularVelocity + "\n" +
            "Rotation: " + transform.eulerAngles
        );

        // WHEEL DIAGNOSTICS
        for (int i = 0; i < wheels.Length; i++)
        {
            WheelCollider wheel = wheels[i];

            if (wheel == null)
            {
                Debug.LogWarning(wheelNames[i] + ": Missing!");
                continue;
            }

            WheelHit hit;
            bool grounded = wheel.GetGroundHit(out hit);

            string report =
                wheelNames[i] +
                " | Grounded: " + grounded +
                " | RPM: " + wheel.rpm.ToString("F2") +
                " | Motor: " + wheel.motorTorque.ToString("F2") +
                " | Brake: " + wheel.brakeTorque.ToString("F2") +
                " | Steer: " + wheel.steerAngle.ToString("F2");

            if (grounded)
            {
                report +=
                    " | Forward Slip: " +
                    hit.forwardSlip.ToString("F3") +
                    " | Side Slip: " +
                    hit.sidewaysSlip.ToString("F3") +
                    " | Ground Force: " +
                    hit.force.ToString("F2") +
                    " | Contact Point: " + hit.point +
                    " | Contact Normal: " + hit.normal;
            }

            Debug.Log(report);
        }

        // CHASSIS COLLIDER
        if (chassisCollider != null)
        {
            Debug.Log(
                "Chassis Collider Enabled: " +
                chassisCollider.enabled +
                " | Bounds Center: " +
                chassisCollider.bounds.center +
                " | Bounds Size: " +
                chassisCollider.bounds.size
            );
        }

        // TERRAIN RAYCAST
        if (checkTerrain)
        {
            RaycastHit terrainHit;

            Vector3 rayOrigin =
                transform.position + Vector3.up * 1000f;

            if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out terrainHit,
                5000f))
            {
                Debug.Log(
                    "Terrain Hit: " +
                    terrainHit.collider.name +
                    " | Distance: " +
                    terrainHit.distance.ToString("F2") +
                    " | Normal: " +
                    terrainHit.normal
                );
            }
            else
            {
                Debug.LogWarning("Terrain Raycast: No hit!");
            }
        }
    }
}
