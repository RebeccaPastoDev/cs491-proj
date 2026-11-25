using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;

public class RoverROSTelemetryReceiver : MonoBehaviour
{
    [Header("ROS Configuration")]
    public string odometryTopic = "/odom_raw";

    [Header("Connection Monitoring")]
    public float telemetryTimeout = 3f;
    public bool logReceivedData = true;
    public float logInterval = 1f;

    [Header("Live ROS Telemetry")]
    public bool hasReceivedData = false;
    public bool telemetryActive = false;

    public Vector3 rosPosition;
    public float rosSpeed;
    public float rosHeading;
    public float rosAngularSpeed;

    private ROSConnection ros;
    private float lastMessageTime = -1f;
    private float lastLogTime = 0f;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        ros.Subscribe<OdometryMsg>(
            odometryTopic,
            ReceiveOdometry
        );

        Debug.Log(
            "[ROS] Subscribed to " + odometryTopic
        );
    }

    void ReceiveOdometry(OdometryMsg message)
    {
        // Position from physical rover odometry.
        // These are ROS coordinates, not Unity world coordinates.
        rosPosition = new Vector3(
            (float)message.pose.pose.position.x,
            (float)message.pose.pose.position.y,
            (float)message.pose.pose.position.z
        );

        // Linear velocity from odometry.
        double vx = message.twist.twist.linear.x;
        double vy = message.twist.twist.linear.y;
        double vz = message.twist.twist.linear.z;

        rosSpeed = Mathf.Sqrt(
            (float)(vx * vx + vy * vy + vz * vz)
        );

        // Angular velocity magnitude in radians per second.
        double wx = message.twist.twist.angular.x;
        double wy = message.twist.twist.angular.y;
        double wz = message.twist.twist.angular.z;

        rosAngularSpeed = Mathf.Sqrt(
            (float)(wx * wx + wy * wy + wz * wz)
        );

        // Heading from ROS orientation quaternion.
        // Assumes standard ROS planar odometry:
        // X forward, Y left, Z up.
        double qx = message.pose.pose.orientation.x;
        double qy = message.pose.pose.orientation.y;
        double qz = message.pose.pose.orientation.z;
        double qw = message.pose.pose.orientation.w;

        double sinYaw = 2.0 * (qw * qz + qx * qy);
        double cosYaw = 1.0 - 2.0 * (qy * qy + qz * qz);

        float yawDegrees =
            (float)(Mathf.Rad2Deg *
            System.Math.Atan2(sinYaw, cosYaw));

        rosHeading = Mathf.Repeat(yawDegrees, 360f);

        hasReceivedData = true;
        telemetryActive = true;
        lastMessageTime = Time.realtimeSinceStartup;

        if (logReceivedData &&
            Time.realtimeSinceStartup - lastLogTime >= logInterval)
        {
            Debug.Log(
                "[ROS TELEMETRY] " +
                "Position: " + rosPosition +
                " | Speed: " + rosSpeed.ToString("F2") + " m/s" +
                " | Heading: " + rosHeading.ToString("F1") + "°"
            );

            lastLogTime = Time.realtimeSinceStartup;
        }
    }

    void Update()
    {
        // Mark telemetry as inactive when messages stop arriving.
        if (hasReceivedData)
        {
            telemetryActive =
                Time.realtimeSinceStartup - lastMessageTime
                <= telemetryTimeout;
        }
    }

    void OnDestroy()
    {
        if (ros != null)
        {
            ros.Unsubscribe(odometryTopic);
        }
    }
}
