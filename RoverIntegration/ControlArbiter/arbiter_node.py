"""ROS 2 interface for the independent control arbiter."""

import json
import os
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

        # Simulation remains the default execution mode.
        self.declare_parameter("execution_mode", "simulation")
        self.execution_mode = self.get_parameter(
            "execution_mode"
        ).value

        if self.execution_mode not in (
            "simulation", "physical_manual"
        ):
            raise RuntimeError("Invalid execution mode")

        self.physical_mode = (
            self.execution_mode == "physical_manual"
        )

        # Prevent accidental cross-domain execution.
        expected_domain = "20" if self.physical_mode else "77"
        expected_localhost = "0" if self.physical_mode else "1"

        if (
            os.environ.get("ROS_DOMAIN_ID") != expected_domain
            or os.environ.get("ROS_LOCALHOST_ONLY") != expected_localhost
        ):
            raise RuntimeError("Incorrect ROS environment for execution mode")

        self.arbiter = ControlArbiter()

        if self.physical_mode:
            self.arbiter.MAX_LINEAR = 0.05
            self.arbiter.MAX_ANGULAR = 0.15

        # Physical output requires explicit startup permission.
        self.declare_parameter("enable_physical_output", False)

        self.physical_output_enabled = (
            self.physical_mode
            and self.get_parameter("enable_physical_output").value
        )

        self.output_topic = (
            "/cmd_vel"
            if self.physical_output_enabled
            else self.OUTPUT_TOPIC
        )

        # Keep only the most recent command in the ROS queue.
        command_qos = QoSProfile(depth=1)

        self.velocity_pub = self.create_publisher(
            Twist, self.output_topic, command_qos
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
            f"Control arbiter mode: {self.execution_mode}"
        )

        self.get_logger().info(
            f"Output topic: {self.output_topic}"
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
            "physical_execution_enabled": self.physical_output_enabled
        })

        self.state_pub.publish(state)

    def authorize_manual(self, request, response):
        # Physical manual authorization must begin from IDLE.
        if self.physical_mode and self.arbiter.state != ControlState.IDLE:
            response.success = False
            response.message = "physical_manual_requires_idle"
            return response

        response.success = self.arbiter.authorize_manual()
        response.message = self.arbiter.reason
        self.publish_output()
        return response

    def authorize_auto(self, request, response):
        # Autonomous execution remains simulation-only.
        if self.physical_mode:
            response.success = False
            response.message = "physical_auto_disabled"
            return response

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
