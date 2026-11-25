"""ROS 2 interface for the independent control arbiter."""

import json
import math

import rclpy

from rclpy.node import Node
from rclpy.qos import QoSProfile, qos_profile_sensor_data

from geometry_msgs.msg import Twist
from nav_msgs.msg import Odometry
from std_msgs.msg import String
from std_srvs.srv import Trigger

from arbiter_core import ControlArbiter, ControlState


class ArbiterNode(Node):

    # Isolated output: never publish directly to the rover
    # during development and integration testing.
    OUTPUT_TOPIC = "/cmd_vel_arbiter_test"
    CONTROL_INTERVAL = 0.05  # seconds (20 Hz)

    def __init__(self):
        super().__init__("control_arbiter")

        self.arbiter = ControlArbiter()

        # Keep only the most recent command in the ROS queue.
        command_qos = QoSProfile(depth=1)

        self.velocity_pub = self.create_publisher(
            Twist, self.OUTPUT_TOPIC, command_qos
        )

        self.state_pub = self.create_publisher(
            String, "/control_state", 10
        )

        self.create_subscription(
            Twist,
            "/cmd_vel_manual",
            self.on_manual,
            command_qos
        )

        self.create_subscription(
            Twist,
            "/cmd_vel_auto",
            self.on_auto,
            command_qos
        )

        self.create_subscription(
            Odometry,
            "/odom_raw",
            self.on_odometry,
            qos_profile_sensor_data
        )

        # Explicit authorization services prevent incoming
        # velocity messages from independently enabling motion.
        self.create_service(
            Trigger,
            "/control/authorize_manual",
            self.authorize_manual
        )

        self.create_service(
            Trigger,
            "/control/authorize_auto",
            self.authorize_auto
        )

        self.create_service(
            Trigger,
            "/control/disarm",
            self.disarm
        )

        self.create_service(
            Trigger,
            "/control/stop",
            self.stop
        )

        self.create_service(
            Trigger,
            "/control/reset_stop",
            self.reset_stop
        )

        self.create_timer(
            self.CONTROL_INTERVAL,
            self.publish_output
        )

        self.get_logger().info(
            "Control arbiter started in TEST MODE"
        )

        self.get_logger().info(
            f"Isolated output: {self.OUTPUT_TOPIC}"
        )

    def on_odometry(self, msg):
        """Check basic odometry validity before refreshing it."""

        position = msg.pose.pose.position
        orientation = msg.pose.pose.orientation
        linear = msg.twist.twist.linear
        angular = msg.twist.twist.angular

        values = (
            position.x, position.y, position.z,
            orientation.x, orientation.y,
            orientation.z, orientation.w,
            linear.x, linear.y, linear.z,
            angular.x, angular.y, angular.z
        )

        norm = math.hypot(
            orientation.x,
            orientation.y,
            orientation.z,
            orientation.w
        )

        if (
            not all(math.isfinite(v) for v in values)
            or abs(norm - 1.0) > 0.05
        ):
            self.arbiter.last_odom = None
            self.arbiter.disarm("invalid_odometry")
            self.publish_output()
            return

        self.arbiter.update_odometry()

    def receive_velocity(self, source, msg):
        # Differential-drive rover supports linear X
        # and angular Z commands only.
        if self.arbiter.state != source:
            return

        unsupported = (
            msg.linear.y,
            msg.linear.z,
            msg.angular.x,
            msg.angular.y
        )

        if any(value != 0.0 for value in unsupported):
            self.arbiter.disarm("unsupported_velocity_axis")
            self.publish_output()
            return

        accepted = self.arbiter.receive_command(
            source,
            msg.linear.x,
            msg.angular.z
        )

        if not accepted:
            self.publish_output()

    def on_manual(self, msg):
        self.receive_velocity(ControlState.MANUAL, msg)

    def on_auto(self, msg):
        self.receive_velocity(ControlState.AUTO, msg)

    def publish_output(self):
        """Evaluate interlocks and publish the permitted command."""

        velocity = self.arbiter.output()

        msg = Twist()
        msg.linear.x = velocity.linear
        msg.angular.z = velocity.angular

        self.velocity_pub.publish(msg)

        state = String()
        state.data = json.dumps({
            "state": self.arbiter.state.value,
            "reason": self.arbiter.reason,
            "odometry_fresh": self.arbiter.odometry_fresh(),
            "linear_m_s": velocity.linear,
            "angular_rad_s": velocity.angular,
            "physical_execution_enabled": False
        })

        self.state_pub.publish(state)

    def authorize_manual(self, request, response):
        response.success = self.arbiter.authorize_manual()
        response.message = self.arbiter.reason
        self.publish_output()
        return response

    def authorize_auto(self, request, response):
        response.success = self.arbiter.authorize_auto()
        response.message = (
            "auto_authorized"
            if response.success
            else "authorization_denied"
        )
        self.publish_output()
        return response

    def disarm(self, request, response):
        self.arbiter.disarm()
        self.publish_output()

        response.success = True
        response.message = "Movement authorization revoked"
        return response

    def stop(self, request, response):
        self.arbiter.stop()
        self.publish_output()

        response.success = True
        response.message = "STOP latched"
        return response

    def reset_stop(self, request, response):
        response.success = self.arbiter.reset_stop()
        response.message = self.arbiter.reason
        self.publish_output()
        return response


def main():
    rclpy.init()
    node = ArbiterNode()

    try:
        # Single-threaded execution keeps arbitration
        # callbacks from modifying state concurrently.
        rclpy.spin(node)

    except KeyboardInterrupt:
        pass

    finally:
        # Best-effort shutdown stop. This cannot protect
        # against a process crash or network failure.
        if rclpy.ok():
            node.arbiter.stop()
            node.publish_output()

        node.destroy_node()

        if rclpy.ok():
            rclpy.shutdown()


if __name__ == "__main__":
    main()
