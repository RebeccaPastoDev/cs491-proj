using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MecanumRoverController : MonoBehaviour
{
    [Header("Rover Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 45f;
    public float acceleration = 4f;

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
    private float forwardInput;
    private float sidewaysInput;
    private float rotationInput;
    private float currentForward;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Keyboard movement controls
        forwardInput = 0f;
        sidewaysInput = 0f;
        rotationInput = 0f;

        if (Input.GetKey(KeyCode.W)) forwardInput += 1f;
        if (Input.GetKey(KeyCode.S)) forwardInput -= 1f;
        if (Input.GetKey(KeyCode.D)) sidewaysInput += 1f;
        if (Input.GetKey(KeyCode.A)) sidewaysInput -= 1f;
        if (Input.GetKey(KeyCode.E)) rotationInput += 1f;
        if (Input.GetKey(KeyCode.Q)) rotationInput -= 1f;
    }

    void FixedUpdate()
    {
        MoveRover();
        AnimateWheels();
    }

    void MoveRover()
    {
        // Forward, backward, and sideways movement
        Vector3 direction = transform.forward * forwardInput + transform.right * sidewaysInput;
        direction = Vector3.ClampMagnitude(direction, 1f);

        Vector3 targetVelocity = direction * moveSpeed;
        Vector3 currentVelocity = rb.linearVelocity;

        // Preserve vertical velocity for gravity
        Vector3 verticalVelocity = Vector3.Project(currentVelocity, transform.up);
        Vector3 horizontalVelocity = currentVelocity - verticalVelocity;

        // Smooth acceleration and deceleration
        Vector3 newHorizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = newHorizontalVelocity + verticalVelocity;

        // Rotate rover body without steering wheels
        rb.MoveRotation(
            rb.rotation * Quaternion.Euler(
                0f,
                rotationInput * rotationSpeed * Time.fixedDeltaTime,
                0f
            )
        );

        // Get actual forward movement speed
        Vector3 localVelocity = transform.InverseTransformDirection(newHorizontalVelocity);
        currentForward = localVelocity.z;
    }

    void AnimateWheels()
    {
        if (!animateWheels) return;

        float radius = Mathf.Max(wheelRadius, 0.001f);
        float wheelSpeed = currentForward / radius;

        // Spin wheels using the parent rover's actual right axis in world space
        Vector3 spinAxis = transform.right;

        SpinWheel(frontLeft, wheelSpeed, spinAxis);
        SpinWheel(frontRight, wheelSpeed, spinAxis);
        SpinWheel(rearLeft, wheelSpeed, spinAxis);
        SpinWheel(rearRight, wheelSpeed, spinAxis);
    }

    void SpinWheel(Transform wheel, float angularSpeed, Vector3 axis)
    {
        if (wheel == null) return;

        float angle = angularSpeed * Mathf.Rad2Deg * Time.fixedDeltaTime * wheelAnimationMultiplier;

        // Rotate around the precise global horizontal axis of the rover body
        wheel.Rotate(axis, angle, Space.World);
    }
}
