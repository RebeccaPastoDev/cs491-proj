import json
import math
from datetime import datetime, timezone

import rclpy
from rclpy.node import Node
from std_msgs.msg import UInt16
from nav_msgs.msg import Odometry
from sensor_msgs.msg import Imu, LaserScan


def vector3(v):
    return {"x": v.x, "y": v.y, "z": v.z}


def quaternion(q):
    return {"x": q.x, "y": q.y, "z": q.z, "w": q.w}


def timestamp(stamp):
    return {"sec": stamp.sec, "nanosec": stamp.nanosec}


def safe_range(value):
    return float(value) if math.isfinite(value) else None


class TelemetryExporter(Node):
    def __init__(self):
        super().__init__("telemetry_exporter")

        self.data = {
            "schema_version": "1.0",
            "source": "yahboom_rover",
            "battery": None,
            "odometry": None,
            "imu": None,
            "lidar": None
        }

        self.create_subscription(
            UInt16, "/battery", self.on_battery, 10
        )
        self.create_subscription(
            Odometry, "/odom_raw", self.on_odometry, 10
        )
        self.create_subscription(
            Imu, "/imu", self.on_imu, 10
        )
        self.create_subscription(
            LaserScan, "/scan", self.on_scan, 10
        )

    def on_battery(self, msg):
        self.data["battery"] = {
            "raw_value": msg.data
        }

    def on_odometry(self, msg):
        self.data["odometry"] = {
            "timestamp": timestamp(msg.header.stamp),
            "frame_id": msg.header.frame_id,
            "child_frame_id": msg.child_frame_id,
            "position_m": vector3(msg.pose.pose.position),
            "orientation": quaternion(msg.pose.pose.orientation),
            "linear_velocity_m_s": vector3(msg.twist.twist.linear),
            "angular_velocity_rad_s": vector3(msg.twist.twist.angular)
        }

    def on_imu(self, msg):
        self.data["imu"] = {
            "timestamp": timestamp(msg.header.stamp),
            "frame_id": msg.header.frame_id,
            "orientation": quaternion(msg.orientation),
            "angular_velocity_rad_s": vector3(msg.angular_velocity),
            "linear_acceleration_m_s2": vector3(msg.linear_acceleration)
        }

    def on_scan(self, msg):
        self.data["lidar"] = {
            "timestamp": timestamp(msg.header.stamp),
            "frame_id": msg.header.frame_id,
            "angle_min_rad": msg.angle_min,
            "angle_max_rad": msg.angle_max,
            "angle_increment_rad": msg.angle_increment,
            "range_min_m": msg.range_min,
            "range_max_m": msg.range_max,
            "ranges_m": [safe_range(v) for v in msg.ranges]
        }


def main():
    rclpy.init()
    node = TelemetryExporter()

    try:
        start = node.get_clock().now()
        while rclpy.ok():
            rclpy.spin_once(node, timeout_sec=0.2)

            if all(value is not None for key, value in node.data.items()
                   if key not in ("schema_version", "source")):
                break

            elapsed = (node.get_clock().now() - start).nanoseconds / 1e9
            if elapsed >= 15:
                break

        node.data["exported_at_utc"] = (
            datetime.now(timezone.utc).isoformat()
        )

        path = "/root/LunarRoverUI/telemetry/telemetry_snapshot.json"
        with open(path, "w") as file:
            json.dump(node.data, file, indent=2, allow_nan=False)

        print("Saved:", path)
        for topic in ("battery", "odometry", "imu", "lidar"):
            status = "RECEIVED" if node.data[topic] is not None else "MISSING"
            print(f"{topic}: {status}")

    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
