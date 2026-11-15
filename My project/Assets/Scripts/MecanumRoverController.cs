using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MecanumRoverController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float acceleration = 4f;
    public float rotationSpeed = 45f;

    [Header("Wheel Meshes")]
    public Transform frontLeft;
    public Transform frontRight;
    public Transform rearLeft;
    public Transform rearRight;

    [Header("Wheel Animation")]
    public bool animateWheels = true;
    public float wheelRadius = 1.19f;
    public float wheelAnimationMultiplier = 1f;

    private Rigidbody rb;

    private float speed;
    private float strafe;
    private float turn;

    private float frontLeftSpeed;
    private float frontRightSpeed;
    private float rearLeftSpeed;
    private float rearRightSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        speed = 0f;
        strafe = 0f;
        turn = 0f;

        // Forward / backward
        if (Input.GetKey(KeyCode.W)) speed += 1f;
        if (Input.GetKey(KeyCode.S)) speed -= 1f;

        // Sideways movement
        if (Input.GetKey(KeyCode.D)) strafe += 1f;
        if (Input.GetKey(KeyCode.A)) strafe -= 1f;

        // Rotation in place
        if (Input.GetKey(KeyCode.E)) turn += 1f;
        if (Input.GetKey(KeyCode.Q)) turn -= 1f;

        // Normalize diagonal movement.
        Vector2 input = Vector2.ClampMagnitude(
            new Vector2(strafe, speed), 1f
        );

        strafe = input.x;
        speed = input.y;

        CalculateWheelSpeeds();
    }

    void CalculateWheelSpeeds()
    {
        // Method 1: Direct mecanum wheel mixing.
        // Standard X roller configuration.

        frontLeftSpeed = speed + strafe + turn;
        frontRightSpeed = speed - strafe - turn;

        rearLeftSpeed = speed - strafe + turn;
        rearRightSpeed = speed + strafe - turn;

        // Normalize wheel commands.
        float max = Mathf.Max(
            1f,
            Mathf.Abs(frontLeftSpeed),
            Mathf.Abs(frontRightSpeed),
            Mathf.Abs(rearLeftSpeed),
            Mathf.Abs(rearRightSpeed)
        );

        frontLeftSpeed /= max;
        frontRightSpeed /= max;
        rearLeftSpeed /= max;
        rearRightSpeed /= max;
    }

    void FixedUpdate()
    {
        MoveRover();
        RotateRover();

        if (animateWheels)
            AnimateWheels();
    }

    void MoveRover()
    {
        Vector3 direction =
            transform.forward * speed +
            transform.right * strafe;

        Vector3 targetVelocity = direction * moveSpeed;

        Vector3 currentVelocity = rb.linearVelocity;

        // Preserve lunar gravity.
        Vector3 verticalVelocity =
            Vector3.Project(currentVelocity, Vector3.up);

        Vector3 horizontalVelocity =
            currentVelocity - verticalVelocity;

        Vector3 newHorizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity =
            newHorizontalVelocity + verticalVelocity;
    }

    void RotateRover()
    {
        if (Mathf.Abs(turn) < 0.01f)
            return;

        float angle =
            turn * rotationSpeed * Time.fixedDeltaTime;

        Quaternion newRotation =
            Quaternion.AngleAxis(angle, Vector3.up)
            * rb.rotation;

        rb.MoveRotation(newRotation);
    }

    void AnimateWheels()
    {
        SpinWheel(frontLeft, frontLeftSpeed);
        SpinWheel(frontRight, frontRightSpeed);
        SpinWheel(rearLeft, rearLeftSpeed);
        SpinWheel(rearRight, rearRightSpeed);
    }

    void SpinWheel(Transform wheel, float wheelSpeed)
    {
        if (wheel == null)
            return;

        float radius = Mathf.Max(wheelRadius, 0.001f);

        float angle =
            (wheelSpeed * moveSpeed / radius) *
            Mathf.Rad2Deg *
            Time.fixedDeltaTime *
            wheelAnimationMultiplier;

        wheel.Rotate(
            transform.right,
            angle,
            Space.World
        );
    }
}
